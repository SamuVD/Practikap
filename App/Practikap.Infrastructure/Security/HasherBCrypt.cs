using Practikap.Application.Common;
using HashBCrypt = BCrypt.Net.BCrypt;

namespace Practikap.Infrastructure.Security;

/// <summary>
/// Implementacion de <see cref="IServicioDeHash"/> con BCrypt (RNF-05).
/// </summary>
/// <remarks>
/// El alias HashBCrypt existe porque el paquete declara una clase BCrypt dentro
/// del espacio de nombres BCrypt.Net: sin el alias, cada llamada tendria que
/// escribirse como BCrypt.Net.BCrypt.HashPassword.
///
/// BCrypt incorpora la sal dentro del hash resultante, de modo que la columna
/// usuarios.contrasena_hash no necesita una columna de sal aparte. El resultado
/// ocupa 60 caracteres, holgado dentro del varchar(255) del Script_DDL.sql.
/// </remarks>
internal sealed class HasherBCrypt : IServicioDeHash
{
    /// <summary>
    /// Costo del algoritmo. Cada unidad duplica el tiempo de calculo: 12 ronda
    /// los 250 ms en hardware corriente, lo bastante lento para encarecer un
    /// ataque por fuerza bruta y lo bastante rapido para RNF-01, que exige
    /// respuesta bajo dos segundos.
    /// </summary>
    private const int FactorDeTrabajo = 12;

    /// <inheritdoc />
    public string Hash(string contrasenaEnClaro)
    {
        if (string.IsNullOrWhiteSpace(contrasenaEnClaro))
            throw new ArgumentException("La contrasena no puede estar vacia.", nameof(contrasenaEnClaro));

        return HashBCrypt.HashPassword(contrasenaEnClaro, FactorDeTrabajo);
    }

    /// <inheritdoc />
    public bool Verificar(string contrasenaEnClaro, string hashAlmacenado)
    {
        if (string.IsNullOrWhiteSpace(contrasenaEnClaro) || string.IsNullOrWhiteSpace(hashAlmacenado))
            return false;

        try
        {
            return HashBCrypt.Verify(contrasenaEnClaro, hashAlmacenado);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash corrupto o escrito por fuera de la aplicacion. Se trata como
            // credencial invalida, no como fallo del servidor.
            return false;
        }
    }
}