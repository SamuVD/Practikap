namespace Practikap.Domain.Exceptions;

/// <summary>
/// El intento de autenticacion no prospero. El middleware la traduce a HTTP 401,
/// conforme al flujo alternativo de CU-01.
/// </summary>
/// <remarks>
/// Se distingue de <see cref="AutorizacionException"/> (403) porque aquella
/// supone una identidad ya establecida que carece de permiso, mientras que esta
/// significa que no llego a establecerse identidad alguna.
///
/// El mensaje nunca indica cual de los dos campos fallo: CU-01 lo exige para no
/// permitir enumeracion de correos registrados.
/// </remarks>
public sealed class CredencialesInvalidasException : DominioException
{
    /// <summary>Crea la excepcion con el motivo del rechazo.</summary>
    /// <param name="mensaje">Texto para el usuario final, sin revelar que campo fallo.</param>
    public CredencialesInvalidasException(string mensaje) : base(mensaje) { }
}