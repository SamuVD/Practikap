using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Notificacion"/>. Modulo M6.
/// </summary>
public interface INotificacionRepository
{
    /// <summary>Lista las notificaciones de un usuario.</summary>
    /// <param name="usuarioId">Usuario destinatario.</param>
    /// <param name="soloNoLeidas">true para devolver unicamente las pendientes de lectura.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con las notificaciones del usuario.</returns>
    Task<IReadOnlyList<Notificacion>> ListarPorUsuarioAsync(int usuarioId, bool soloNoLeidas, CancellationToken ct);

    /// <summary>
    /// Registra una notificacion nueva. Cuando la origina el Motor de Reglas
    /// conserva la referencia a la regla que la disparo, conforme a RN-09.
    /// </summary>
    /// <param name="notificacion">Notificacion a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado a la notificacion.</returns>
    Task<int> AgregarAsync(Notificacion notificacion, CancellationToken ct);

    /// <summary>Marca una notificacion como leida.</summary>
    /// <param name="id">Identificador de la notificacion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task MarcarLeidaAsync(int id, CancellationToken ct);
}
