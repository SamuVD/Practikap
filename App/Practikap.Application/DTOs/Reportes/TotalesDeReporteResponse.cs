namespace Practikap.Application.DTOs.Reportes;

/// <summary>
/// Bloque de totales del contenido de un reporte (RF-08, O9).
/// </summary>
/// <remarks>
/// La distribucion trae <b>siempre las cuatro claves</b> de EstadoPractica, en cero
/// las que ninguna practica ocupa. Omitir las vacias habria producido un JSON mas
/// corto a cambio de obligar a cada consumidor a comprobar la existencia de la
/// clave antes de leerla, y de que un grafico de barras perdiera columnas segun
/// los datos. Las claves son los nombres de los miembros, no los literales de la
/// columna, con el criterio de H31: EnCurso y EnRiesgo, no "En curso" ni "En
/// riesgo".
///
/// El promedio general es la media de los promedios del <b>instructor</b> sobre las
/// practicas que tienen al menos una calificacion computable. Los del aprendiz no
/// entran: RN-10 declara las dos direcciones independientes, y promediarlas juntas
/// mezclaria dos escalas que el sistema mantiene separadas desde el Script_DDL.sql.
/// Es ademas la direccion que el Motor de Reglas evalua para el umbral de riesgo
/// (RN-09), de modo que el numero que resume un reporte es el mismo que gobierna
/// las alertas.
///
/// Las practicas sin calificaciones computables no bajan ese promedio: quedan
/// fuera del divisor en lugar de entrar como cero. Un grupo recien iniciado
/// tendria si no un promedio general cercano a cero que no describiria nada.
/// </remarks>
/// <param name="CantidadDePracticas">Numero de practicas consolidadas en el reporte.</param>
/// <param name="DistribucionPorEstado">Practicas por estado. Las cuatro claves de EstadoPractica, en cero las ausentes.</param>
/// <param name="PromedioGeneral">Media de los promedios del instructor sobre las practicas con calificaciones computables. Cero si no hay ninguna.</param>
public sealed record TotalesDeReporteResponse
(
    int CantidadDePracticas,
    IReadOnlyDictionary<string, int> DistribucionPorEstado,
    decimal PromedioGeneral
);
