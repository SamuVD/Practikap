using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso al catalogo de <see cref="Rol"/>. Modulo M1.
/// El catalogo se siembra en la base de datos y no se modifica en operacion.
/// </summary>
public interface IRolRepository
{
    /// <summary>Lista los roles del sistema.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con los roles disponibles.</returns>
    Task<IReadOnlyList<Rol>> ListarAsync(CancellationToken ct);

    /// <summary>Obtiene un rol por su nombre.</summary>
    /// <param name="nombre">Nombre del rol: Administrador, Instructor o Aprendiz.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El rol, o null si no existe.</returns>
    Task<Rol?> ObtenerPorNombreAsync(string nombre, CancellationToken ct);
}
