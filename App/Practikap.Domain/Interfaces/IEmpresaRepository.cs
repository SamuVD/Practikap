using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Empresa"/>. Modulo M3.
/// </summary>
public interface IEmpresaRepository
{
    /// <summary>Obtiene una empresa por su identificador.</summary>
    /// <param name="id">Identificador de la empresa.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La empresa, o null si no existe.</returns>
    Task<Empresa?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Indica si el NIT ya esta registrado. El caso de uso traduce un resultado
    /// positivo a HTTP 409.
    /// </summary>
    /// <param name="nit">NIT a verificar.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>true si el NIT ya existe; false en caso contrario.</returns>
    Task<bool> ExisteNitAsync(string nit, CancellationToken ct);

    /// <summary>Lista las empresas registradas.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con las empresas.</returns>
    Task<IReadOnlyList<Empresa>> ListarAsync(CancellationToken ct);

    /// <summary>Registra una empresa nueva.</summary>
    /// <param name="empresa">Empresa a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado a la empresa.</returns>
    Task<int> AgregarAsync(Empresa empresa, CancellationToken ct);
}
