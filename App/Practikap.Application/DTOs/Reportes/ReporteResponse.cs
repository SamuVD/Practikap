namespace Practikap.Application.DTOs.Reportes;

/// <summary>
/// Un reporte completo: su rastro y su contenido consolidado (RF-08, O9, O14).
/// Salida de POST /api/reportes y de GET /api/reportes/{id}.
/// </summary>
/// <remarks>
/// El rastro es lo que quedo escrito en la tabla reportes; el contenido se
/// recompone en cada consulta sobre las practicas vinculadas, con los datos
/// actuales (O14). Los dos no son lo mismo y la diferencia es deliberada: un
/// reporte guarda que se pregunto y sobre que practicas, no una fotografia de
/// como estaban. Consultar hoy un reporte de hace un mes muestra las mismas
/// practicas con sus estados y promedios de hoy.
///
/// Filtros viaja como el texto JSON tal cual se persistio, sin deserializar de
/// vuelta a su forma tipada. Es el rastro literal: lo que se devuelve es
/// exactamente lo que dice la columna, y una fila escrita a mano en MySQL por
/// fuera de la API no puede tumbar el GET.
///
/// FechaGeneracion es nullable y ReporteResumenResponse.FechaGeneracion no lo es.
/// La diferencia tiene un solo caso: cuando el filtro no selecciona ninguna
/// practica, O8 manda responder 200 con el contenido vacio y <b>sin persistir
/// nada</b>. Ese reporte no existe en la base, su Id vale cero y no tiene fecha.
/// Rellenarla con el valor por defecto de DateTime habria puesto un 0001-01-01 en
/// el JSON, que es una fecha falsa; null dice lo que pasa.
/// </remarks>
/// <param name="Id">Identificador del reporte. Cero cuando el filtro no selecciono ninguna practica y no se persistio nada (O8).</param>
/// <param name="Tipo">Alcance funcional declarado, como texto (H31).</param>
/// <param name="Filtros">Criterios aplicados, en el mismo JSON que guarda la columna reportes.filtros.</param>
/// <param name="GeneradoPor">Identificador del usuario que lo genero.</param>
/// <param name="FechaGeneracion">Momento de la generacion, fijado por MySQL. Nulo cuando el reporte no se persistio (O8).</param>
/// <param name="Lineas">Una entrada por practica consolidada, ascendente por identificador de practica.</param>
/// <param name="Totales">Bloque de agregados sobre esas mismas practicas.</param>
public sealed record ReporteResponse
(
    int Id,
    string Tipo,
    string Filtros,
    int GeneradoPor,
    DateTime? FechaGeneracion,
    IReadOnlyList<LineaDeReporteResponse> Lineas,
    TotalesDeReporteResponse Totales
);
