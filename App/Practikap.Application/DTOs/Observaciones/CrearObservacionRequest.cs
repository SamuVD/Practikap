namespace Practikap.Application.DTOs.Observaciones;

/// <summary>
/// Datos de entrada para registrar una observacion sobre un seguimiento.
/// </summary>
/// <remarks>
/// No lleva SeguimientoId: el identificador viaja en la ruta anidada
/// POST /api/seguimientos/{id}/observaciones, y duplicarlo en el cuerpo abriria
/// la posibilidad de que ambos discreparan.
///
/// Como el de seguimientos, no declara fecha (RN-11) ni marca de anulacion (RN-12).
/// </remarks>
/// <param name="Contenido">Texto de la observacion.</param>
public sealed record CrearObservacionRequest
(
    string Contenido
);
