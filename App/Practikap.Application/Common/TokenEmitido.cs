namespace Practikap.Application.Common;

/// <summary>
/// Resultado de emitir un JWT.
/// </summary>
/// <param name="Token">Cadena firmada que el cliente envia en Authorization: Bearer.</param>
/// <param name="ReferenciaToken">
/// Claim jti. Es el valor que se guarda en tokens_revocados.referencia_token si
/// el token se invalida antes de expirar (RN-03).
/// </param>
/// <param name="ExpiraEn">Momento de expiracion en UTC.</param>
public sealed record TokenEmitido(string Token, string ReferenciaToken, DateTime ExpiraEn);
