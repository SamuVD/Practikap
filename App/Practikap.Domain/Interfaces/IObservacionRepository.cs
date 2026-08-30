using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Observacion"/>. Modulo M4.
/// Igual que el de seguimientos, no expone eliminacion ni edicion de contenido
/// (RN-12), y por el mismo motivo que documenta <see cref="ISeguimientoRepository"/>.
/// </summary>
public interface IObservacionRepository
{
    /// <summary>Obtiene una observacion por su identificador.</summary>
    /// <param name="id">Identificador de la observacion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La observacion, o null si no existe.</returns>
    /// <remarks>
    /// Lo agrego I9: sin el, el caso de uso de anulacion no tiene forma de
    /// cargar la entidad sobre la que invocar Observacion.Anular.
    /// </remarks>
    Task<Observacion?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Lista las observaciones asociadas a un seguimiento.</summary>
    /// <param name="seguimientoId">Seguimiento cuyas observaciones se consultan.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con las observaciones del seguimiento.</returns>
    Task<IReadOnlyList<Observacion>> ListarPorSeguimientoAsync(int seguimientoId, CancellationToken ct);

    /// <summary>Registra una observacion nueva.</summary>
    /// <param name="observacion">Observacion a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado a la observacion.</returns>
    Task<int> AgregarAsync(Observacion observacion, CancellationToken ct);

    /// <summary>
    /// Registra una observacion que llega desatada. Es la via por la que se
    /// persiste la marca de anulacion, unica alteracion que RN-12 permite.
    /// </summary>
    /// <param name="observacion">Observacion ya modificada por el Dominio.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task ActualizarAsync(Observacion observacion, CancellationToken ct);
}
