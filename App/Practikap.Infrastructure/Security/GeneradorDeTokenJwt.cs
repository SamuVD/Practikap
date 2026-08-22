using System.Globalization;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Practikap.Application.Common;

namespace Practikap.Infrastructure.Security;

/// <summary>
/// Emisor de JWT firmados con HS256, conforme al flujo del Doc_Tecnico 3.2.
/// </summary>
/// <remarks>
/// Los claims se declaran con sus nombres cortos de RFC 7519 (sub, role, jti) y
/// no con las URI largas de ClaimTypes: son las que espera el frontend y las que
/// documenta el Doc_Tecnico. Por eso la validacion desactiva el mapeo automatico
/// de claims entrantes (ver DependencyInjection).
///
/// Los tipos de Microsoft.IdentityModel llegan como dependencia transitiva de
/// Microsoft.AspNetCore.Authentication.JwtBearer, que esta capa ya referencia.
/// </remarks>
internal sealed class GeneradorDeTokenJwt : IGeneradorDeToken
{
    private readonly OpcionesJwt _opciones;

    /// <summary>Crea el generador con las opciones ya validadas en el arranque.</summary>
    /// <param name="opciones">Parametros de emision del token.</param>
    public GeneradorDeTokenJwt(OpcionesJwt opciones) => _opciones = opciones;

    /// <inheritdoc />
    public TokenEmitido Generar(int usuarioId, string correo, string rol)
    {
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.SecretKey));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        // El jti es un GUID de 36 caracteres, que es exactamente el ancho de la
        // columna tokens_revocados.referencia_token.
        var referencia = Guid.NewGuid().ToString();
        var emision = DateTime.UtcNow;
        var expiracion = emision.AddMinutes(_opciones.ExpirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _opciones.Issuer,
            Audience = _opciones.Audience,
            IssuedAt = emision,
            NotBefore = emision,
            Expires = expiracion,
            SigningCredentials = credenciales,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = usuarioId.ToString(CultureInfo.InvariantCulture),
                ["role"] = rol,
                ["jti"] = referencia,
                ["email"] = correo
            }
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new TokenEmitido(token, referencia, expiracion);
    }
}
