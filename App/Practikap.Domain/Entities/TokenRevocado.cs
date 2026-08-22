using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Token JWT invalidado antes de su expiracion natural. Implementa el registro
/// que RN-03 exige para que el middleware rechace tokens de sesiones cerradas
/// aunque su firma siga siendo valida.
/// </summary>
/// <remarks>
/// Entidad de solo escritura y lectura: una vez registrada no se modifica.
/// </remarks>
public class TokenRevocado
{
    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private TokenRevocado() { }

    /// <summary>Registra la revocacion de un token.</summary>
    /// <param name="usuarioId">Usuario propietario del token.</param>
    /// <param name="referenciaToken">Valor del claim jti del JWT.</param>
    /// <param name="motivo">Causa de la revocacion.</param>
    /// <exception cref="ReglaDeDominioException">Si falta el usuario o la referencia.</exception>
    public TokenRevocado(int usuarioId, string referenciaToken, MotivoRevocacion motivo)
    {
        if (usuarioId <= 0)
            throw new ReglaDeDominioException("La revocacion requiere un usuario valido.", "RN-03");
        if (string.IsNullOrWhiteSpace(referenciaToken))
            throw new ReglaDeDominioException("La referencia del token es obligatoria.", "RN-03");

        UsuarioId = usuarioId;
        ReferenciaToken = referenciaToken.Trim();
        Motivo = motivo;
    }

    /// <summary>Identificador. Columna tokens_revocados.id.</summary>
    public int Id { get; private set; }

    /// <summary>Usuario propietario del token. Columna tokens_revocados.usuario_id.</summary>
    public int UsuarioId { get; private set; }

    /// <summary>Claim jti del JWT. Columna tokens_revocados.referencia_token.</summary>
    public string ReferenciaToken { get; private set; } = null!;

    /// <summary>Causa de la revocacion. Columna tokens_revocados.motivo.</summary>
    public MotivoRevocacion Motivo { get; private set; }

    /// <summary>
    /// Momento de la revocacion. La genera MySQL con DEFAULT CURRENT_TIMESTAMP.
    /// </summary>
    public DateTime FechaRevocacion { get; private set; }

    /// <summary>Usuario propietario del token revocado.</summary>
    public Usuario Usuario { get; private set; } = null!;
}
