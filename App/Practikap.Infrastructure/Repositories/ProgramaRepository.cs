using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="IProgramaRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Se implementa una sola vez y la consumen
/// M3, que consulta, y M8, que administra.
/// </summary>
internal sealed class ProgramaRepository : IProgramaRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public ProgramaRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// Se rastrea, sin AsNoTracking: es la consulta sobre la que M8 aplicara
    /// Programa.Actualizar en el paso 4.9.
    /// </remarks>
    public Task<Programa?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        _contexto.Programas
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <inheritdoc />
    /// <remarks>
    /// Normaliza con Trim porque el constructor de Programa tambien lo aplica, y
    /// asi la verificacion cubre de verdad uq_programas_nombre. Mismo criterio
    /// que EmpresaRepository.ExisteNitAsync.
    /// </remarks>
    public Task<bool> ExisteNombreAsync(string nombre, CancellationToken ct) =>
        _contexto.Programas
            .AsNoTracking()
            .AnyAsync(p => p.Nombre == nombre.Trim(), ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Programa>> ListarAsync(CancellationToken ct) =>
        await _contexto.Programas
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02): el Id que devuelve solo queda poblado despues de
    /// que el caso de uso llame IUnidadDeTrabajo.GuardarCambiosAsync.
    /// </remarks>
    public Task<int> AgregarAsync(Programa programa, CancellationToken ct)
    {
        _contexto.Programas.Add(programa);
        return Task.FromResult(programa.Id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// El caso de uso tipico obtiene el programa con ObtenerPorIdAsync (queda
    /// rastreado), lo modifica con Programa.Actualizar y EF Core detecta el
    /// cambio sin llamada adicional. Este metodo solo actua cuando el programa
    /// llega desatado, para no depender de que el llamador siempre use la
    /// instancia rastreada. Mismo criterio que UsuarioRepository.ActualizarAsync.
    /// </remarks>
    public Task ActualizarAsync(Programa programa, CancellationToken ct)
    {
        if (_contexto.Entry(programa).State == EntityState.Detached)
            _contexto.Programas.Update(programa);

        return Task.CompletedTask;
    }
}
