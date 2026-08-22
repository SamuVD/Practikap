using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de la lista de revocacion sobre tokens_revocados (RN-03).
/// </summary>
/// <remarks>
/// Es el unico repositorio concreto que se implementa en el Paso 3.1. Los demas
/// llegan en la Fase 4, modulo por modulo. La excepcion es deliberada: este lo
/// consume el propio pipeline de autenticacion y no un caso de uso, tal como
/// anota el Doc_Arquitectura 5.2 para tokens_revocados.
/// </remarks>
internal sealed class TokenRevocadoRepository : ITokenRevocadoRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public TokenRevocadoRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// No llama SaveChanges: la confirmacion es responsabilidad del caso de uso
    /// que cierra la sesion, segun ADR-02.
    /// </remarks>
    public Task RegistrarAsync(TokenRevocado token, CancellationToken ct)
    {
        _contexto.TokensRevocados.Add(token);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Consulta sin seguimiento: se ejecuta en cada peticion protegida y su
    /// resultado no se modifica, asi que rastrearlo solo costaria memoria.
    /// </remarks>
    public Task<bool> EstaRevocadoAsync(string referenciaToken, CancellationToken ct) =>
        _contexto.TokensRevocados
            .AsNoTracking()
            .AnyAsync(token => token.ReferenciaToken == referenciaToken, ct);
}
