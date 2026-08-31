using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Reportes;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Reportes;

/// <summary>
/// Lista el historico de reportes generados que el solicitante puede ver
/// (RF-08, CU-07, O1, RN-13).
/// </summary>
/// <remarks>
/// GET /api/reportes no desaparecio al llegar el POST: cambio de significado
/// (O1). Antes de este paso la ruta no existia en el codigo y la Matriz_de_Roles
/// la describia como la consulta que producia un reporte; ahora producir es del
/// POST y esta ruta lista lo ya producido. Es lo que convierte a M7 en un modulo
/// con rastro y no en un generador sin memoria.
///
/// Los dos alcances de RN-13 se resuelven eligiendo el metodo de repositorio, con
/// el mismo patron de ListarPracticasUseCase: el Administrador ve el historico
/// completo y el Instructor solo los reportes que genero el mismo. No hay un
/// tercer caso porque O3 deja al Aprendiz fuera del modulo.
///
/// Devuelve el rastro sin el contenido. Recomponer las lineas de cada reporte
/// obligaria a cargar el grafo de todas sus practicas y a calcular sus promedios
/// para devolver algo que el listado no muestra.
/// </remarks>
public sealed class ListarReportesUseCase
{
    private readonly IReporteRepository _reporteRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="reporteRepo">Acceso al historico de reportes.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarReportesUseCase(
        IReporteRepository reporteRepo, IContextoUsuario contexto, IMapper mapeador)
    {
        _reporteRepo = reporteRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve los reportes del alcance del solicitante.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El rastro de cada reporte, del mas reciente al mas antiguo.</returns>
    /// <exception cref="AutorizacionException">Si el alcance del token no es Global ni Asignado (403).</exception>
    public async Task<IReadOnlyList<ReporteResumenResponse>> ExecuteAsync(CancellationToken ct)
    {
        var reportes = _contexto.Alcance switch
        {
            AlcanceConsulta.Global => await _reporteRepo.ListarTodosAsync(ct),
            AlcanceConsulta.Asignado =>
                await _reporteRepo.ListarPorGeneradorAsync(_contexto.UsuarioId, ct),
            _ => throw new AutorizacionException(
                "El rol autenticado no tiene acceso a la consulta de reportes.")
        };

        return _mapeador.Map<IReadOnlyList<ReporteResumenResponse>>(reportes);
    }
}
