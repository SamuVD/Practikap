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
/// O14, y es lo que permite que la Ronda 2 exporte un reporte antiguo sin haber
/// guardado nunca un CSV.
///
/// <b>El reporte ajeno responde 404 y no 403.</b> Un Instructor que pide el reporte
/// de otro recibe la misma respuesta que si el identificador no existiera, porque
/// distinguir los dos casos le diria que el recurso existe fuera de su alcance,
/// que es exactamente lo que RN-13 evita. Es el mismo criterio con que O13 hace
/// que un filtro fuera de alcance devuelva vacio en lugar de prohibido.
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
    /// <returns>El rastro del reporte y el contenido de las practicas que consolido.</returns>
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

        var identificadores = reporte.Practicas.Select(practica => practica.Id).ToList();
        var promediosInstructor =
            await _calificacionInstructorRepo.PromediosPorPracticasAsync(identificadores, ct);
        var promediosAprendiz =
            await _calificacionAprendizRepo.PromediosPorPracticasAsync(identificadores, ct);

        var (lineas, totales) = ArmadorDeReporte.Componer(
            reporte.Practicas, promediosInstructor, promediosAprendiz);

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
