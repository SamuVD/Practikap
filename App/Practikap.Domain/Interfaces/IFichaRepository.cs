using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Ficha"/>. Modulo M3.
/// </summary>
public interface IFichaRepository
{
    /// <summary>Obtiene una ficha por su identificador.</summary>
    /// <param name="id">Identificador de la ficha.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La ficha, o null si no existe.</returns>
    Task<Ficha?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Obtiene una ficha por su numero.</summary>
    /// <param name="numeroFicha">Numero unico de la ficha.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La ficha, o null si no existe.</returns>
    Task<Ficha?> ObtenerPorNumeroAsync(string numeroFicha, CancellationToken ct);

    /// <summary>Lista las fichas de un programa de formacion.</summary>
    /// <param name="programaId">Programa al que pertenecen las fichas.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con las fichas del programa.</returns>
    Task<IReadOnlyList<Ficha>> ListarPorProgramaAsync(int programaId, CancellationToken ct);
}
