namespace Practikap.Application.DTOs.Calificaciones;

/// <summary>
/// Datos de entrada para registrar una calificacion sobre una practica, en
/// cualquiera de las dos direcciones.
/// </summary>
/// <remarks>
/// Un solo DTO para los dos POST. La forma de entrada es identica en ambas
/// direcciones y la direccion la fija la ruta —/instructor o /aprendiz—, no el
/// cuerpo. Un discriminador aqui seria justo el atributo tipo que el
/// Script_DDL.sql descarto al separar las dos tablas, y ademas seria falsificable
/// por el cliente: la direccion la decide el endpoint y el rol del token.
///
/// No declara ningun campo de fecha, y esa ausencia es deliberada: RN-11 exige
/// que la marca de tiempo la determine el servidor, y la forma mas solida de
/// garantizarlo no es descartar la fecha del cliente sino no ofrecerle nunca
/// donde escribirla. La columna la genera MySQL con DEFAULT CURRENT_TIMESTAMP.
///
/// Tampoco declara Anulado ni AnuladoPor: una calificacion nace vigente y solo el
/// Administrador puede marcarla, por una via distinta (RN-12).
/// </remarks>
/// <param name="PracticaId">Practica sobre la que se registra la calificacion.</param>
/// <param name="Valor">Valor de la calificacion, entre 0.0 y 5.0 con un decimal.</param>
/// <param name="Comentario">Comentario cualitativo. Opcional.</param>
public sealed record CrearCalificacionRequest
(
    int PracticaId,
    decimal Valor,
    string? Comentario = null
);
