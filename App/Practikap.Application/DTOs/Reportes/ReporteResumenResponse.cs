namespace Practikap.Application.DTOs.Reportes;

/// <summary>
/// El rastro de un reporte generado, sin su contenido (RF-08, O1). Salida de
/// GET /api/reportes.
/// </summary>
/// <remarks>
/// El listado es el historico de reportes generados, no un catalogo de
/// contenidos: recomponer las lineas y los totales de cada fila obligaria a
/// cargar el grafo de todas las practicas de todos los reportes y a calcular los
/// promedios de cada uno para devolver algo que nadie pidio. Quien quiera el
/// contenido de uno concreto pide GET /api/reportes/{id}.
///
/// Filtros viaja como el texto JSON tal cual se persistio, igual que en
/// ReporteResponse y por la misma razon.
///
/// FechaGeneracion no es nullable aqui, a diferencia de ReporteResponse: todo
/// reporte que aparece en este listado esta persistido, y por tanto MySQL le
/// escribio una fecha.
/// </remarks>
/// <param name="Id">Identificador del reporte.</param>
/// <param name="Tipo">Alcance funcional declarado, como texto (H31).</param>
/// <param name="Filtros">Criterios aplicados, en el mismo JSON que guarda la columna reportes.filtros.</param>
/// <param name="GeneradoPor">Identificador del usuario que lo genero.</param>
/// <param name="FechaGeneracion">Momento de la generacion, fijado por MySQL.</param>
public sealed record ReporteResumenResponse
(
    int Id,
    string Tipo,
    string Filtros,
    int GeneradoPor,
    DateTime FechaGeneracion
);
