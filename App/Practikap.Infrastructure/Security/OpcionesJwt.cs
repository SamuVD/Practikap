using System.Text;
using Microsoft.Extensions.Configuration;

namespace Practikap.Infrastructure.Security;

/// <summary>
/// Parametros de emision y validacion del JWT, leidos de la seccion Jwt de la
/// configuracion (Doc_Tecnico 6.2).
/// </summary>
/// <remarks>
/// Se leen con el indexador de IConfiguration y no con Get&lt;T&gt;() a proposito:
/// el enlace por reflexion vive en Microsoft.Extensions.Configuration.Binder, un
/// paquete que esta capa no referencia. Leer cuatro claves a mano evita agregar
/// una dependencia por comodidad.
/// </remarks>
public sealed class OpcionesJwt
{
    /// <summary>Nombre de la seccion en la configuracion.</summary>
    public const string Seccion = "Jwt";

    /// <summary>
    /// Longitud minima de la clave en bytes. HS256 es HMAC-SHA256 y una clave
    /// mas corta que su salida debilita la firma sin que nada falle a la vista.
    /// </summary>
    private const int BytesMinimosDeClave = 32;

    private OpcionesJwt(string secretKey, string issuer, string audience, int expirationMinutes)
    {
        SecretKey = secretKey;
        Issuer = issuer;
        Audience = audience;
        ExpirationMinutes = expirationMinutes;
    }

    /// <summary>Clave de firma simetrica. Secreto: nunca se versiona ni se registra.</summary>
    public string SecretKey { get; }

    /// <summary>Emisor esperado del token. Claim iss.</summary>
    public string Issuer { get; }

    /// <summary>Destinatario esperado del token. Claim aud.</summary>
    public string Audience { get; }

    /// <summary>Vigencia del token en minutos (RNF-04).</summary>
    public int ExpirationMinutes { get; }

    /// <summary>
    /// Lee y valida la seccion Jwt. Falla en el arranque y no en la primera
    /// peticion: una API que responde 500 a todo porque le falta la clave es
    /// peor que una API que no arranca (Doc_Tecnico 5.11, nivel Critical).
    /// </summary>
    /// <param name="configuration">Configuracion de la aplicacion.</param>
    /// <returns>Las opciones ya validadas.</returns>
    /// <exception cref="InvalidOperationException">Si falta o es invalida alguna clave.</exception>
    public static OpcionesJwt Leer(IConfiguration configuration)
    {
        var secretKey = configuration[$"{Seccion}:SecretKey"];
        var issuer = configuration[$"{Seccion}:Issuer"];
        var audience = configuration[$"{Seccion}:Audience"];
        var minutos = configuration[$"{Seccion}:ExpirationMinutes"];

        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException(Falta("Jwt:SecretKey", esSecreto: true));

        if (Encoding.UTF8.GetByteCount(secretKey) < BytesMinimosDeClave)
            throw new InvalidOperationException(
                $"La clave 'Jwt:SecretKey' tiene menos de {BytesMinimosDeClave} bytes ({BytesMinimosDeClave * 8} bits).\n" +
                "HS256 exige al menos esa longitud. Genere una nueva con: openssl rand -base64 48");

        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException(Falta("Jwt:Issuer", esSecreto: false));

        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException(Falta("Jwt:Audience", esSecreto: false));

        if (!int.TryParse(minutos, out var expiracion) || expiracion <= 0)
            throw new InvalidOperationException(
                "La clave 'Jwt:ExpirationMinutes' debe ser un entero mayor que cero.");

        return new OpcionesJwt(secretKey, issuer, audience, expiracion);
    }

    private static string Falta(string clave, bool esSecreto) =>
        $"La clave de configuracion '{clave}' esta vacia o no existe.\n" +
        (esSecreto
            ? "Agreguela al archivo App/Practikap.API/appsettings.Development.local.json,\n" +
              "que esta ignorado por .gitignore y nunca debe commitearse.\n" +
              "Genere el valor con: openssl rand -base64 48"
            : "Agreguela al archivo App/Practikap.API/appsettings.Development.json.\n" +
              "No es un secreto: se versiona.");
}
