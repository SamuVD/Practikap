using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Reportes;
using Practikap.Application.Validators.Practicas;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;
using Practikap.Domain.ValueObjects;

namespace Practikap.Application.UseCases.Reportes;

/// <summary>
/// Genera un reporte sobre las practicas que un filtro selecciona, lo persiste
/// con su rastro y devuelve su contenido consolidado (RF-08, CU-07, RN-13).
/// </summary>
/// <remarks>
/// El alcance de RN-13 se resuelve primero, eligiendo el metodo de repositorio, y
/// los nueve criterios se aplican despues, sobre la coleccion ya restringida. Ese
/// orden es lo que hace que un filtro fuera del alcance devuelva un reporte vacio
/// con 200 en lugar de 403 (O13): el Instructor que filtra por un aprendiz que no
/// es suyo no llega a saber si ese aprendiz tiene practicas, que es justo lo que
/// RN-13 persigue. Es el mismo orden y la misma razon de ListarPracticasUseCase.
///
/// Los filtros viven aqui y no en el repositorio (O4, H27): IReporteRepository
/// perdio ConsolidarAsync y no gana parametros de filtro. Filtrar en memoria es
/// ademas lo que permite derivar ProgramaId de la ficha sin una consulta mas,
/// porque el grafo ya viene cargado.
///
/// El Aprendiz no entra en este modulo (O3). Su alcance cae en el descarte del
/// switch junto con cualquier rol desconocido, y da 403.
/// </remarks>
public sealed class GenerarReporteUseCase
{
    private readonly IPracticaRepository _practicaRepo;
    private readonly IReporteRepository _reporteRepo;
    private readonly ICalificacionInstructorRepository _calificacionInstructorRepo;
    private readonly ICalificacionAprendizRepository _calificacionAprendizRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<GenerarReporteRequest> _validador;
    private readonly ILogger<GenerarReporteUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="practicaRepo">Acceso a practicas, para el conjunto base y las instancias rastreadas.</param>
    /// <param name="reporteRepo">Persistencia del reporte y sus vinculos.</param>
    /// <param name="calificacionInstructorRepo">Promedios de la direccion Instructor hacia Aprendiz.</param>
    /// <param name="calificacionAprendizRepo">Promedios de la direccion Aprendiz hacia Instructor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto unico de confirmacion (ADR-02).</param>
    /// <param name="validador">Validacion de forma del DTO de entrada.</param>
    /// <param name="registro">Registro de la generacion.</param>
    public GenerarReporteUseCase(
        IPracticaRepository practicaRepo,
        IReporteRepository reporteRepo,
        ICalificacionInstructorRepository calificacionInstructorRepo,
        ICalificacionAprendizRepository calificacionAprendizRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<GenerarReporteRequest> validador,
        ILogger<GenerarReporteUseCase> registro)
    {
        _practicaRepo = practicaRepo;
        _reporteRepo = reporteRepo;
        _calificacionInstructorRepo = calificacionInstructorRepo;
        _calificacionAprendizRepo = calificacionAprendizRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _registro = registro;
    }

    /// <summary>Genera el reporte y devuelve su rastro con su contenido.</summary>
    /// <param name="request">Tipo declarado y criterios de seleccion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>
    /// El reporte generado. Si el filtro no selecciono ninguna practica, un
    /// reporte con Id en cero, sin lineas y con totales en cero, que no se
    /// persistio (O8).
    /// </returns>
    /// <exception cref="ValidationException">Si el tipo no es uno de los dos o el rango de fechas esta invertido (400).</exception>
    /// <exception cref="AutorizacionException">Si el alcance del token no es Global ni Asignado (403).</exception>
    /// <exception cref="ReglaDeDominioException">Si el estado o la modalidad del filtro traen un literal desconocido, o si la composicion no es coherente con el tipo declarado (422).</exception>
    public async Task<ReporteResponse> ExecuteAsync(
        GenerarReporteRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        var practicas = _contexto.Alcance switch
        {
            AlcanceConsulta.Global => await _practicaRepo.ListarTodasAsync(ct),
            AlcanceConsulta.Asignado =>
                await _practicaRepo.ListarPorInstructorAsync(_contexto.UsuarioId, ct),
            // El Aprendiz cae aqui junto con cualquier rol desconocido: O3 lo deja
            // fuera de M7 entero, incluida la lectura.
            _ => throw new AutorizacionException(
                "El rol autenticado no tiene acceso a la generacion de reportes.")
        };

        // El validador ya confirmo que el literal del tipo es uno de los dos (H31).
        var tipo = Enum.Parse<TipoReporte>(request.Tipo);
        var filtro = TraducirFiltro(request.Filtro);
        var filtros = SerializadorDeFiltro.Serializar(filtro);

        var seleccionadas = Aplicar(practicas, filtro).ToList();

        if (seleccionadas.Count == 0)
        {
            // O8: no se persiste nada y no se llama ni a AgregarAsync ni a
            // GuardarCambiosAsync. Un filtro que no selecciona practicas no es un
            // error, es una respuesta.
            var (sinLineas, sinTotales) = ArmadorDeReporte.Componer(
                [], new Dictionary<int, decimal>(), new Dictionary<int, decimal>());

            _registro.LogInformation(
                "El filtro de reporte de tipo {TipoReporte} solicitado por el usuario "
                + "{UsuarioId} no selecciono ninguna practica: no se persistio nada.",
                tipo, _contexto.UsuarioId);

            return new ReporteResponse(
                0, tipo.ToString(), filtros, _contexto.UsuarioId, null, sinLineas, sinTotales);
        }

        var reporte = new Reporte(tipo, filtros, _contexto.UsuarioId);

        // Las instancias rastreadas y no las de seleccionadas, que vienen con
        // AsNoTracking: una practica desatada en la coleccion del reporte quedaria
        // marcada Added y EF Core intentaria reinsertarla.
        var rastreadas = await _practicaRepo.ListarPorIdsAsync(
            seleccionadas.Select(practica => practica.Id), ct);

        foreach (var practica in rastreadas)
            reporte.VincularPractica(practica);

        // La comprobacion va despues de vincular y no antes, porque
        // ComposicionEsCoherente lee la coleccion del reporte: sobre un agregado
        // recien construido el conteo es cero y los dos tipos darian false. En
        // este camino todavia no se ha llamado a AgregarAsync, de modo que el
        // reporte nunca entra en el contexto y el 422 no deja nada escrito.
        if (!reporte.ComposicionEsCoherente())
            throw new ReglaDeDominioException(
                $"El filtro selecciono {seleccionadas.Count} practicas y un reporte de tipo "
                + $"{tipo} {(tipo == TipoReporte.Individual
                    ? "consolida exactamente una"
                    : "consolida al menos una")}. "
                + "Ajuste el filtro o el tipo declarado.",
                "RF-08");

        await _reporteRepo.AgregarAsync(reporte, ct);

        // Una sola confirmacion para las dos tablas (O12). El INSERT en reportes y
        // los de reporte_practica caen en el mismo SaveChanges, que el DbContext ya
        // envuelve en su transaccion: IUnidadDeTrabajo no necesita control
        // transaccional explicito aunque su propio comentario lo previera.
        // Hasta aqui reporte.Id vale 0 y FechaGeneracion es el valor por defecto de
        // DateTime; la confirmacion asigna el primero y trae de vuelta la que
        // escribio MySQL (RN-11).
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        var identificadores = seleccionadas.Select(practica => practica.Id).ToList();
        var promediosInstructor =
            await _calificacionInstructorRepo.PromediosPorPracticasAsync(identificadores, ct);
        var promediosAprendiz =
            await _calificacionAprendizRepo.PromediosPorPracticasAsync(identificadores, ct);

        // Se compone sobre seleccionadas y no sobre rastreadas: aquellas traen el
        // grafo cargado por el listado de alcance y estas no lo traen.
        var (lineas, totales) = ArmadorDeReporte.Componer(
            seleccionadas, promediosInstructor, promediosAprendiz);

        _registro.LogInformation(
            "Reporte {ReporteId} de tipo {TipoReporte} generado por el usuario {UsuarioId} "
            + "sobre {CantidadDePracticas} practicas.",
            reporte.Id, tipo, _contexto.UsuarioId, seleccionadas.Count);

        return new ReporteResponse(
            reporte.Id,
            reporte.Tipo.ToString(),
            reporte.Filtros,
            reporte.GeneradoPor,
            reporte.FechaGeneracion,
            lineas,
            totales);
    }

    /// <summary>
    /// Traduce el DTO de filtro a su objeto de valor, comprobando los dos
    /// literales de enumerado que viajan como texto.
    /// </summary>
    /// <remarks>
    /// Un literal desconocido no es un filtro fuera de alcance sino una solicitud
    /// que no se puede procesar: 422, no reporte vacio (O19). Se compara contra
    /// los nombres del enumerado, igual que en el validador y por la misma razon
    /// que documenta ReglasDeEnumerado (H31).
    ///
    /// Un filtro ausente se traduce al objeto de valor sin criterios, que
    /// selecciona todo el alcance del solicitante.
    /// </remarks>
    private static FiltroReporte TraducirFiltro(FiltroReporteRequest? request)
    {
        if (request is null)
            return new FiltroReporte();

        EstadoPractica? estado = null;

        if (!string.IsNullOrWhiteSpace(request.Estado))
        {
            if (!Enum.GetNames<EstadoPractica>().Contains(request.Estado, StringComparer.Ordinal))
                throw new ReglaDeDominioException(
                    $"El estado debe ser uno de estos cuatro: {ReglasDeEnumerado.EstadosAdmitidos}.",
                    "RN-05");

            estado = Enum.Parse<EstadoPractica>(request.Estado);
        }

        ModalidadPractica? modalidad = null;

        if (!string.IsNullOrWhiteSpace(request.Modalidad))
        {
            if (!Enum.GetNames<ModalidadPractica>()
                     .Contains(request.Modalidad, StringComparer.Ordinal))
                throw new ReglaDeDominioException(
                    "La modalidad debe ser una de estas cuatro: "
                    + $"{ReglasDeEnumerado.ModalidadesAdmitidas}.",
                    "RF-03");

            modalidad = Enum.Parse<ModalidadPractica>(request.Modalidad);
        }

        return new FiltroReporte
        {
            InstructorId = request.InstructorId,
            AprendizId = request.AprendizId,
            FichaId = request.FichaId,
            ProgramaId = request.ProgramaId,
            EmpresaId = request.EmpresaId,
            Estado = estado,
            Modalidad = modalidad,
            Desde = request.Desde,
            Hasta = request.Hasta
        };
    }

    /// <summary>
    /// Aplica los nueve criterios en memoria, combinados con Y logico, sobre la
    /// coleccion ya restringida por el alcance del solicitante.
    /// </summary>
    /// <remarks>
    /// ProgramaId se deriva de practica.Ficha.ProgramaId: la practica no guarda
    /// programa_id, se resuelve via ficha_id para mantener la tercera forma
    /// normal. El grafo ya lo trae cargado, asi que el filtro no cuesta una
    /// consulta mas. Es lo mismo que hace el filtro programaId de H19.
    ///
    /// Desde y Hasta acotan la fecha de inicio y son inclusivos en los dos
    /// extremos, tal como documenta FiltroReporte.
    /// </remarks>
    private static IEnumerable<Practica> Aplicar(
        IEnumerable<Practica> practicas, FiltroReporte filtro)
    {
        var filtradas = practicas;

        if (filtro.InstructorId is not null)
            filtradas = filtradas.Where(p => p.InstructorId == filtro.InstructorId.Value);

        if (filtro.AprendizId is not null)
            filtradas = filtradas.Where(p => p.AprendizId == filtro.AprendizId.Value);

        if (filtro.FichaId is not null)
            filtradas = filtradas.Where(p => p.FichaId == filtro.FichaId.Value);

        if (filtro.ProgramaId is not null)
            filtradas = filtradas.Where(p => p.Ficha.ProgramaId == filtro.ProgramaId.Value);

        if (filtro.EmpresaId is not null)
            filtradas = filtradas.Where(p => p.EmpresaId == filtro.EmpresaId.Value);

        if (filtro.Estado is not null)
            filtradas = filtradas.Where(p => p.Estado == filtro.Estado.Value);

        if (filtro.Modalidad is not null)
            filtradas = filtradas.Where(p => p.Modalidad == filtro.Modalidad.Value);

        if (filtro.Desde is not null)
            filtradas = filtradas.Where(p => p.FechaInicio >= filtro.Desde.Value);

        if (filtro.Hasta is not null)
            filtradas = filtradas.Where(p => p.FechaInicio <= filtro.Hasta.Value);

        return filtradas;
    }
}
