namespace Practikap.Application.DTOs.Reportes;

/// <summary>Generacion de un reporte sobre practicas (RF-08, O1, O7).</summary>
/// <remarks>
/// El tipo viaja en el cuerpo y no se deriva del numero de practicas que el
/// filtro seleccione (O7). Derivarlo habria hecho imposible equivocarse, y esa es
/// justamente la razon para no hacerlo: un reporte Individual que devuelve tres
/// practicas significa que el filtro no era el que el solicitante creia, y el 422
/// se lo dice. Con la derivacion, el mismo error habria producido un reporte
/// Grupal silencioso.
///
/// No lleva GeneradoPor. El usuario que lo genera sale del token, no del cuerpo:
/// aceptarlo de fuera permitiria atribuir una consulta a otra cuenta y romperia
/// el rastro que este modulo existe para dejar.
///
/// No lleva la fecha. La pone MySQL con DEFAULT CURRENT_TIMESTAMP (RN-11).
/// </remarks>
/// <param name="Tipo">Alcance funcional del reporte, como texto: Individual o Grupal (H31).</param>
/// <param name="Filtro">Criterios de seleccion. Nulo equivale a un filtro sin criterios, es decir todo el alcance del solicitante.</param>
public sealed record GenerarReporteRequest
(
    string Tipo,
    FiltroReporteRequest? Filtro
);
