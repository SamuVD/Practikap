using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Notificacion"/>. Modulo M6.
/// </summary>
/// <remarks>
/// El paso 3.1 declaraba un MarcarLeidaAsync que recibia el identificador y
/// habria obligado al repositorio a cargar la notificacion y llamarle
/// MarcarLeida, es decir a invocar dominio. Se reemplazo por ObtenerPorIdAsync y
/// ActualizarAsync, y la marca la aplica el caso de uso (L8). Extiende a M6 el
/// criterio de H28, I9, J7 y de la decision equivalente de la serie K sobre
/// IMensajeRepository.
/// </remarks>
public interface INotificacionRepository
{
    /// <summary>Obtiene una notificacion por su identificador.</summary>
    /// <param name="id">Identificador de la notificacion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La notificacion, o null si no existe.</returns>
    Task<Notificacion?> ObtenerPorIdAsync(int id, CancellationToken ct);

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

    /// <summary>Registra el cambio de una notificacion ya existente.</summary>
    /// <param name="notificacion">Notificacion con su estado modificado.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task ActualizarAsync(Notificacion notificacion, CancellationToken ct);
}
