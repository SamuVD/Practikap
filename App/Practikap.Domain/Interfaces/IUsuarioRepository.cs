using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a datos de <see cref="Usuario"/>. Modulo M1.
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>Obtiene un usuario por su identificador.</summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El usuario, o null si no existe.</returns>
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Obtiene un usuario por su correo. Es la consulta del inicio de sesion.</summary>
    /// <param name="correo">Correo institucional.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El usuario, o null si no existe.</returns>
    Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken ct);

    /// <summary>
    /// Indica si el correo ya esta registrado. El caso de uso traduce un
    /// resultado positivo a HTTP 409.
    /// </summary>
    /// <param name="correo">Correo a verificar.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>true si el correo ya existe; false en caso contrario.</returns>
    Task<bool> ExisteCorreoAsync(string correo, CancellationToken ct);

    /// <summary>Lista todos los usuarios del sistema.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con todos los usuarios.</returns>
    /// <remarks>
    /// Alcance Global de RN-13: solo el Administrador puede consumirla. El caso
    /// de uso comprueba el rol antes de llamarla.
    /// </remarks>
    Task<IReadOnlyList<Usuario>> ListarTodosAsync(CancellationToken ct);

    /// <summary>Lista los usuarios que tienen un rol determinado.</summary>
    /// <param name="rolId">Rol por el que se filtra.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con los usuarios del rol.</returns>
    Task<IReadOnlyList<Usuario>> ListarPorRolAsync(int rolId, CancellationToken ct);

    /// <summary>Registra un usuario nuevo.</summary>
    /// <param name="usuario">Usuario a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado al usuario.</returns>
    Task<int> AgregarAsync(Usuario usuario, CancellationToken ct);

    /// <summary>Registra los cambios efectuados sobre un usuario existente.</summary>
    /// <param name="usuario">Usuario modificado.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task ActualizarAsync(Usuario usuario, CancellationToken ct);

    /// <summary>
    /// Cambia el rol de un usuario. Materializa la operacion de administracion
    /// que RN-01 reserva al Administrador.
    /// </summary>
    /// <param name="usuarioId">Usuario afectado.</param>
    /// <param name="rolId">Rol destino.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task CambiarRolAsync(int usuarioId, int rolId, CancellationToken ct);
}
