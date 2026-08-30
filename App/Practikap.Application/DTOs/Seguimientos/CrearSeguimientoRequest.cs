namespace Practikap.Application.DTOs.Seguimientos;

/// <summary>
/// Datos de entrada para registrar un seguimiento sobre una practica.
/// </summary>
/// <remarks>
/// No declara ningun campo de fecha, y esa ausencia es deliberada: RN-11 exige
/// que la marca de tiempo la determine el servidor, y la forma mas solida de
/// garantizarlo no es descartar la fecha del cliente sino no ofrecerle nunca
/// donde escribirla. La columna la genera MySQL con DEFAULT CURRENT_TIMESTAMP.
///
/// Tampoco declara Anulado ni AnuladoPor: un seguimiento nace vigente y solo el
/// Administrador puede marcarlo, por una via distinta (RN-12).
/// </remarks>
/// <param name="PracticaId">Practica sobre la que se registra el avance.</param>
/// <param name="Avance">Descripcion del avance observado.</param>
/// <param name="Etapa">Etapa de la practica a la que corresponde el avance.</param>
public sealed record CrearSeguimientoRequest
(
    int PracticaId,
    string Avance,
    string Etapa
);
