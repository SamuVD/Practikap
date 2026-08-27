using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="IRolRepository"/> sobre el catalogo sembrado
/// por RolConfiguration.HasData. Modulo M1.
/// </summary>
internal sealed class RolRepository : IRolRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public RolRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Rol>> ListarAsync(CancellationToken ct) =>
        await _contexto.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<Rol?> ObtenerPorNombreAsync(string nombre, CancellationToken ct) =>
        _contexto.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Nombre == nombre, ct);
}