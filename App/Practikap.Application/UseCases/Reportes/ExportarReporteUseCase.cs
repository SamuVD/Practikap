using Practikap.Application.DTOs.Reportes;
using Practikap.Domain.Exceptions;

namespace Practikap.Application.UseCases.Reportes;

/// <summary>
/// Exporta un reporte a CSV descargable (RF-08, CU-07, HU-08b, O21, O23).
/// </summary>
/// <remarks>
/// <b>No obtiene el reporte: se lo pide a ObtenerReporteUseCase</b> (O23). Su unica
/// dependencia es ese caso de uso, y eso es todo lo que este archivo decide. No
/// repite la carga, ni la guarda del 404, ni el filtrado de alcance de O20, ni la
/// composicion del contenido.
///
/// La razon es la misma por la que existe ArmadorDeReporte. Si la exportacion
/// cargara el reporte por su cuenta, habria dos definiciones de "el contenido de un
/// reporte para este solicitante", y el dia que una cambiara —una guarda nueva, un
/// promedio calculado de otro modo, un alcance mas fino— el CSV empezaria a
/// discrepar del JSON sin que nadie lo notara, porque nada compara los dos. Con la
/// composicion directa la discrepancia es imposible: el archivo se formatea sobre
/// el mismo ReporteResponse que GET /api/reportes/{id} devuelve.
///
/// Componerlos asi no cuesta nada: los dos son Scoped y los dos estan registrados
/// (ADR-02, ADR-05), de modo que comparten el DbContext de la peticion.
///
/// <b>Tres desviaciones respecto de la convencion de cuatro lineas</b> del
/// Doc_Tecnico 5.2, las tres por la misma causa —es una lectura que delega—:
///
/// <list type="bullet">
/// <item>No hay ValidateAndThrowAsync porque no recibe DTO de entrada: el
/// identificador viaja en la ruta y la restriccion :int lo cubre.</item>
/// <item>No hay GuardarCambiosAsync porque no registra ningun cambio.</item>
/// <item>Las excepciones no se capturan ni se traducen: AutorizacionException y
/// NoEncontradoException suben tal cual desde el caso de uso delegado, de modo que
/// exportar un reporte ajeno responde 404 por exactamente el mismo camino con el
/// que lo responde consultarlo.</item>
/// </list>
///
/// Lo que si cumple es CancellationToken recibido y propagado, y la clase sealed.
/// </remarks>
public sealed class ExportarReporteUseCase
{
    private readonly ObtenerReporteUseCase _obtener;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="obtener">Consulta que resuelve el reporte, su alcance y su contenido.</param>
    public ExportarReporteUseCase(ObtenerReporteUseCase obtener) => _obtener = obtener;

    /// <summary>Devuelve el reporte formateado como archivo CSV.</summary>
    /// <param name="id">Identificador del reporte.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El archivo, con su nombre, su tipo de contenido y sus bytes.</returns>
    /// <exception cref="AutorizacionException">Si el alcance del token no es Global ni Asignado (403).</exception>
    /// <exception cref="NoEncontradoException">Si el reporte no existe, o si lo genero otro usuario y el solicitante es Instructor (404).</exception>
    public async Task<ArchivoExportado> ExecuteAsync(int id, CancellationToken ct)
    {
        var reporte = await _obtener.ExecuteAsync(id, ct);

        return FormateadorCsv.Formatear(reporte);
    }
}
