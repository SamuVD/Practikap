using Practikap.Application.Common;

namespace Practikap.Infrastructure.Persistence;

/// <summary>
/// Implementacion de <see cref="IUnidadDeTrabajo"/> sobre
/// <see cref="PractikapDbContext"/>.
/// </summary>
/// <remarks>
/// Es deliberadamente delgada: no agrega logica propia. Su unica razon de
/// existir es que la capa de Aplicacion pueda confirmar sin referenciar EF Core.
///
/// Se registra con alcance Scoped, el mismo del DbContext (ADR-02): comparten
/// instancia dentro de la peticion, asi que confirma exactamente los cambios que
/// los repositorios de esa misma peticion registraron.
/// </remarks>
internal sealed class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea la unidad de trabajo sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped.</param>
    public UnidadDeTrabajo(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public Task<int> GuardarCambiosAsync(CancellationToken ct) =>
        _contexto.SaveChangesAsync(ct);
}