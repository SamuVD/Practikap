using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Observacion"/>. Modulo M4.
/// Igual que el de seguimientos, no expone actualizacion ni eliminacion (RN-12).
/// </summary>
public interface IObservacionRepository
{
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
    /// Marca una observacion como anulada. Unica alteracion permitida por
    /// RN-12, reservada al Administrador.
    /// </summary>
    /// <param name="id">Identificador de la observacion.</param>
    /// <param name="anuladoPorId">Administrador que ejecuta la anulacion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task AnularAsync(int id, int anuladoPorId, CancellationToken ct);
}
