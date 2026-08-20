using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="TokenRevocado"/>. Modulo M1.
/// Sostiene la lista de revocacion que consulta el middleware de autenticacion.
/// </summary>
public interface ITokenRevocadoRepository
{
    /// <summary>
    /// Registra la revocacion de un token. Implementa la invalidacion que
    /// RN-03 exige al cerrar sesion o cambiar la contrasena.
    /// </summary>
    /// <param name="token">Registro de revocacion a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task RegistrarAsync(TokenRevocado token, CancellationToken ct);

    /// <summary>
    /// Indica si un token esta revocado. El middleware solo consulta este
    /// metodo cuando la firma del JWT ya resulto valida (RN-03).
    /// </summary>
    /// <param name="referenciaToken">Valor del claim jti del JWT.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>true si el token fue revocado; false en caso contrario.</returns>
    Task<bool> EstaRevocadoAsync(string referenciaToken, CancellationToken ct);
}
