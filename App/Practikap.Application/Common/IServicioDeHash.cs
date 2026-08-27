namespace Practikap.Application.Common;

/// <summary>
/// Derivacion y verificacion de contrasenas. Cumple RNF-05: la aplicacion nunca
/// almacena ni compara contrasenas en texto plano.
/// </summary>
/// <remarks>
/// El contrato no nombra el algoritmo. Un caso de uso que verifica credenciales
/// depende de esta interfaz y no de BCrypt, de modo que un cambio de algoritmo
/// —o el aumento del factor de trabajo— no toque la capa de Aplicacion.
/// </remarks>
public interface IServicioDeHash
{
    /// <summary>Deriva el hash que se persiste en usuarios.contrasena_hash.</summary>
    /// <param name="contrasenaEnClaro">Contrasena tal como la escribio el usuario.</param>
    /// <returns>Hash con su sal incorporada, apto para varchar(255).</returns>
    /// <exception cref="ArgumentException">Si la contrasena viene vacia.</exception>
    string Hash(string contrasenaEnClaro);

    /// <summary>
    /// Comprueba una contrasena contra un hash almacenado.
    /// </summary>
    /// <param name="contrasenaEnClaro">Contrasena recibida en la solicitud.</param>
    /// <param name="hashAlmacenado">Hash guardado en la base de datos.</param>
    /// <returns>true si coinciden; false en cualquier otro caso.</returns>
    /// <remarks>
    /// Devuelve false en lugar de lanzar cuando el hash esta ausente o
    /// malformado: el caso de uso de inicio de sesion debe responder 401 sin
    /// revelar cual de los dos campos fallo (flujo alternativo de CU-01), y una
    /// excepcion produciria un 500 que si distinguiria ambos casos.
    /// </remarks>
    bool Verificar(string contrasenaEnClaro, string hashAlmacenado);
}