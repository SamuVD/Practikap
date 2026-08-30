using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="IEmpresaRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M3.
/// </summary>
internal sealed class EmpresaRepository : IEmpresaRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public EmpresaRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public Task<Empresa?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        _contexto.Empresas
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    /// <inheritdoc />
    /// <remarks>
    /// Normaliza con Trim porque el constructor de Empresa tambien lo aplica: sin
    /// esto, un NIT con espacios al margen esquivaria la verificacion y despues
    /// chocaria contra uq_empresas_nit con un 500 en vez del 409 previsto.
    /// Mismo criterio que UsuarioRepository.ExisteCorreoAsync.
    /// </remarks>
    public Task<bool> ExisteNitAsync(string nit, CancellationToken ct) =>
        _contexto.Empresas
            .AsNoTracking()
            .AnyAsync(e => e.Nit == nit.Trim(), ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Empresa>> ListarAsync(CancellationToken ct) =>
        await _contexto.Empresas
            .AsNoTracking()
            .OrderBy(e => e.RazonSocial)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02): el Id que devuelve solo queda poblado despues de
    /// que el caso de uso llame IUnidadDeTrabajo.GuardarCambiosAsync.
    /// </remarks>
    public Task<int> AgregarAsync(Empresa empresa, CancellationToken ct)
    {
        _contexto.Empresas.Add(empresa);
        return Task.FromResult(empresa.Id);
    }
}
