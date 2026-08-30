using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Programa"/>. Se implementa una sola vez y se
/// inyecta en M3, que lo consulta, y en M8, que lo administra.
/// </summary>
public interface IProgramaRepository
{
    /// <summary>Obtiene un programa por su identificador.</summary>
    /// <param name="id">Identificador del programa.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El programa, o null si no existe.</returns>
    Task<Programa?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Indica si el nombre ya esta registrado. El caso de uso traduce un
    /// resultado positivo a HTTP 409.
    /// </summary>
    /// <param name="nombre">Nombre a verificar.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>true si el nombre ya existe; false en caso contrario.</returns>
    Task<bool> ExisteNombreAsync(string nombre, CancellationToken ct);

    /// <summary>Lista los programas de formacion.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con los programas.</returns>
    Task<IReadOnlyList<Programa>> ListarAsync(CancellationToken ct);

    /// <summary>Registra un programa nuevo.</summary>
    /// <param name="programa">Programa a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado al programa.</returns>
    Task<int> AgregarAsync(Programa programa, CancellationToken ct);

    /// <summary>Registra los cambios efectuados sobre un programa existente.</summary>
    /// <param name="programa">Programa modificado.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task ActualizarAsync(Programa programa, CancellationToken ct);
}
