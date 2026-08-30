namespace Practikap.Application.DTOs.Observaciones;

/// <summary>
/// Representacion de salida de una observacion.
/// </summary>
/// <remarks>
/// Sale anidada dentro de <see cref="Seguimientos.SeguimientoResponse"/> en las
/// dos consultas del historial (I5), y suelta como respuesta del PATCH de
/// anulacion.
///
/// Anulado y AnuladoPor siguen el mismo criterio que en el seguimiento: se
/// devuelven siempre (I4) y el actor es un identificador desnudo, porque la
/// configuracion mapea anulado_por sin propiedad de navegacion.
///
/// Que la anulacion de un seguimiento no cambie este campo en sus observaciones
/// es I11: cada registro se anula por separado y conserva la traza de quien lo
/// anulo, que es lo que RN-12 protege.
/// </remarks>
/// <param name="Id">Identificador de la observacion.</param>
/// <param name="SeguimientoId">Seguimiento al que pertenece.</param>
/// <param name="Contenido">Texto de la observacion.</param>
/// <param name="FechaRegistro">Momento del registro, determinado por el servidor (RN-11).</param>
/// <param name="Anulado">Marca de anulacion.</param>
/// <param name="AnuladoPor">Identificador del Administrador que anulo, nulo si el registro esta vigente.</param>
public sealed record ObservacionResponse
(
    int Id,
    int SeguimientoId,
    string Contenido,
    DateTime FechaRegistro,
    bool Anulado,
    int? AnuladoPor
);
