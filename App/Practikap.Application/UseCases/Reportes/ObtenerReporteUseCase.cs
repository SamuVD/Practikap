using Practikap.Application.Common;
using Practikap.Application.DTOs.Reportes;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Reportes;

/// <summary>
/// Consulta un reporte generado y recompone su contenido sobre las practicas que
/// consolido, con los datos actuales (RF-08, CU-07, O14, RN-13).
/// </summary>
/// <remarks>
/// El contenido no se guarda, se recompone. La tabla reportes guarda el rastro
/// —que se pregunto, con que filtros y quien— y reporte_practica guarda sobre que
/// practicas; los nombres, los estados y los promedios se leen en cada consulta.
/// Consultar hoy un reporte de hace un mes muestra las mismas practicas con sus
/// estados y promedios de hoy, no una fotografia del pasado. Esa es la lectura de
/// O14, y es lo que permite que ExportarReporteUseCase entregue el CSV de un
/// reporte antiguo sin haber guardado nunca un archivo.
///
/// <b>El reporte ajeno responde 404 y no 403.</b> Un Instructor que pide el reporte
/// de otro recibe la misma respuesta que si el identificador no existiera, porque
/// distinguir los dos casos le diria que el recurso existe fuera de su alcance,
/// que es exactamente lo que RN-13 evita. Es el mismo criterio con que O13 hace
/// que un filtro fuera de alcance devuelva vacio en lugar de prohibido.
///
/// <b>El alcance se comprueba dos veces, y la segunda no es redundante</b> (O20).
/// La primera pregunta de quien es el reporte; la segunda, de quien son hoy las
/// practicas que consolido. Hacen falta las dos porque O14 recompone el contenido
/// con los datos actuales: un reporte generado antes de que el Administrador
/// reasignara una practica (RN-04) la seguiria mostrando, con el instructor nuevo
/// y los promedios de hoy, y el Instructor original conservaria una ventana en
/// vivo sobre una practica que GET /api/practicas ya le niega. No seria una fuga
/// historica —que el rastro nombre la practica es correcto—, sino continua.
///
/// Lo que se filtra es la lectura, no el rastro: reporte_practica queda intacta y
/// el Administrador sigue viendo el reporte entero. Si el filtrado deja el
/// conjunto vacio la respuesta es 200 con lineas vacias y totales en cero, no 404,
/// por la razon inversa a la del parrafo anterior: el reporte existe y es suyo, y
/// lo que dejo de ser suyo es su contenido. Negarlo entero afirmaria que la
/// consulta nunca ocurrio, que es justo lo que M7 existe para conservar.
///
/// Es lectura pura: no valida DTO de entrada, no registra cambios y no confirma.
/// </remarks>
public sealed class ObtenerReporteUseCase
{
    private readonly IReporteRepository _reporteRepo;
    private readonly ICalificacionInstructorRepository _calificacionInstructorRepo;
    private readonly ICalificacionAprendizRepository _calificacionAprendizRepo;
    private readonly IContextoUsuario _contexto;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="reporteRepo">Acceso al reporte y a sus practicas consolidadas.</param>
    /// <param name="calificacionInstructorRepo">Promedios de la direccion Instructor hacia Aprendiz.</param>
    /// <param name="calificacionAprendizRepo">Promedios de la direccion Aprendiz hacia Instructor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    public ObtenerReporteUseCase(
        IReporteRepository reporteRepo,
        ICalificacionInstructorRepository calificacionInstructorRepo,
        ICalificacionAprendizRepository calificacionAprendizRepo,
        IContextoUsuario contexto)
    {
        _reporteRepo = reporteRepo;
        _calificacionInstructorRepo = calificacionInstructorRepo;
        _calificacionAprendizRepo = calificacionAprendizRepo;
        _contexto = contexto;
    }

    /// <summary>Devuelve el reporte con su contenido recompuesto.</summary>
    /// <param name="id">Identificador del reporte.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>
    /// El rastro del reporte y el contenido de las practicas que consolido, acotado
    /// a las que el solicitante puede ver hoy (O20). Para el Administrador son
    /// todas; para el Instructor, las que sigan siendo suyas.
    /// </returns>
    /// <exception cref="AutorizacionException">Si el alcance del token no es Global ni Asignado (403).</exception>
    /// <exception cref="NoEncontradoException">Si el reporte no existe, o si lo genero otro usuario y el solicitante es Instructor (404).</exception>
    public async Task<ReporteResponse> ExecuteAsync(int id, CancellationToken ct)
    {
        if (_contexto.Alcance is not (AlcanceConsulta.Global or AlcanceConsulta.Asignado))
            throw new AutorizacionException(
                "El rol autenticado no tiene acceso a la consulta de reportes.");

        var reporte = await _reporteRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Reporte", id);

        // Fuera del alcance del Instructor, el reporte se comporta como si no
        // existiera: la misma excepcion y por tanto el mismo 404 (RN-13).
        if (_contexto.Alcance == AlcanceConsulta.Asignado
            && reporte.GeneradoPor != _contexto.UsuarioId)
            throw new NoEncontradoException("Reporte", id);

        // Segunda medicion del alcance, esta vez sobre las practicas y contra la
        // asignacion de hoy, no contra la del dia en que se genero el reporte
        // (O20, RN-13). Filtra la lectura y no el rastro: reporte.Practicas viene
        // de una consulta AsNoTracking y reporte_practica no se toca.
        var visibles = _contexto.Alcance == AlcanceConsulta.Asignado
            ? reporte.Practicas
                .Where(practica => practica.InstructorId == _contexto.UsuarioId)
                .ToList()
            : reporte.Practicas.ToList();

        // Los identificadores salen de las visibles y no de las consolidadas: pedir
        // el promedio de una practica que no se va a mostrar seria leer de la base
        // justo lo que RN-13 prohibe entregar.
        var identificadores = visibles.Select(practica => practica.Id).ToList();
        var promediosInstructor =
            await _calificacionInstructorRepo.PromediosPorPracticasAsync(identificadores, ct);
        var promediosAprendiz =
            await _calificacionAprendizRepo.PromediosPorPracticasAsync(identificadores, ct);

        var (lineas, totales) = ArmadorDeReporte.Componer(
            visibles, promediosInstructor, promediosAprendiz);

        return new ReporteResponse(
            reporte.Id,
            reporte.Tipo.ToString(),
            reporte.Filtros,
            reporte.GeneradoPor,
            reporte.FechaGeneracion,
            lineas,
            totales);
    }
}
