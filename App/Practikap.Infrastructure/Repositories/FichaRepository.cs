using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="IFichaRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M3.
/// </summary>
internal sealed class FichaRepository : IFichaRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public FichaRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>Incluye el Programa: todo consumidor necesita su nombre, no solo el Id.</remarks>
    public Task<Ficha?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        _contexto.Fichas
            .Include(f => f.Programa)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    /// <inheritdoc />
    /// <remarks>
    /// Es la consulta con la que el caso de uso de alta verifica
    /// uq_fichas_numero antes de insertar, y por eso normaliza igual que el
    /// constructor de Ficha, que aplica Trim al numero.
    /// </remarks>
    public Task<Ficha?> ObtenerPorNumeroAsync(string numeroFicha, CancellationToken ct) =>
        _contexto.Fichas
            .AsNoTracking()
            .Include(f => f.Programa)
            .FirstOrDefaultAsync(f => f.NumeroFicha == numeroFicha.Trim(), ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Ficha>> ListarPorProgramaAsync(int programaId, CancellationToken ct) =>
        await _contexto.Fichas
            .AsNoTracking()
            .Include(f => f.Programa)
            .Where(f => f.ProgramaId == programaId)
            .OrderBy(f => f.NumeroFicha)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Ficha>> ListarAsync(CancellationToken ct) =>
        await _contexto.Fichas
            .AsNoTracking()
            .Include(f => f.Programa)
            .OrderBy(f => f.NumeroFicha)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02): el Id que devuelve solo queda poblado despues de
    /// que el caso de uso llame IUnidadDeTrabajo.GuardarCambiosAsync.
    /// </remarks>
    public Task<int> AgregarAsync(Ficha ficha, CancellationToken ct)
    {
        _contexto.Fichas.Add(ficha);
        return Task.FromResult(ficha.Id);
    }
}
