using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="IPracticaRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M3.
/// </summary>
/// <remarks>
/// Los tres metodos de listado corresponden a los tres alcances de RN-13. El
/// repositorio no decide cual se ejecuta: esa eleccion es del caso de uso, sobre
/// IContextoUsuario (ADR-03).
/// </remarks>
internal sealed class PracticaRepository : IPracticaRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public PracticaRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// Se rastrea, sin AsNoTracking: es la consulta que alimenta los tres PATCH
    /// de M3, que modifican la practica obtenida y confian en que EF Core detecte
    /// el cambio. Mismo criterio que UsuarioRepository.ObtenerPorCorreoAsync.
    /// </remarks>
    public Task<Practica?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        ConGrafoCompleto(_contexto.Practicas)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Practica>> ListarPorInstructorAsync(int instructorId, CancellationToken ct) =>
        await ConGrafoCompleto(_contexto.Practicas.AsNoTracking())
            .Where(p => p.InstructorId == instructorId)
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Practica>> ListarPorAprendizAsync(int aprendizId, CancellationToken ct) =>
        await ConGrafoCompleto(_contexto.Practicas.AsNoTracking())
            .Where(p => p.AprendizId == aprendizId)
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Practica>> ListarTodasAsync(CancellationToken ct) =>
        await ConGrafoCompleto(_contexto.Practicas.AsNoTracking())
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// Compara contra Finalizada en lugar de usar Practica.EstaActiva, que esta
    /// marcada como Ignore en PracticaConfiguration y no tiene traduccion a SQL.
    /// </remarks>
    public Task<bool> TieneActivaAsync(int aprendizId, CancellationToken ct) =>
        _contexto.Practicas
            .AsNoTracking()
            .AnyAsync(p => p.AprendizId == aprendizId
                        && p.Estado != EstadoPractica.Finalizada, ct);

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02): el Id que devuelve solo queda poblado despues de
    /// que el caso de uso llame IUnidadDeTrabajo.GuardarCambiosAsync. Se lee
    /// practica.Id sobre la misma instancia tras confirmar.
    /// </remarks>
    public Task<int> AgregarAsync(Practica practica, CancellationToken ct)
    {
        _contexto.Practicas.Add(practica);
        return Task.FromResult(practica.Id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Invoca el metodo de dominio, que es quien evalua RN-05. El flag va en true
    /// porque H17 reserva PATCH /api/practicas/{id}/estado al Administrador, unico
    /// rol autorizado a retroceder un estado. El Motor de Reglas del paso 4.7 no
    /// pasa por aqui: usa Practica.MarcarEnRiesgo sobre la entidad (H9).
    /// Sigue el patron de UsuarioRepository.CambiarRolAsync.
    /// </remarks>
    public async Task ActualizarEstadoAsync(int id, EstadoPractica estado, CancellationToken ct)
    {
        var practica = await _contexto.Practicas.FindAsync(new object?[] { id }, ct)
            ?? throw new NoEncontradoException($"No existe una practica con Id {id}.");

        practica.CambiarEstado(estado, esAdministrador: true);
    }

    /// <inheritdoc />
    /// <remarks>
    /// La verificacion de practica activa duplicada que exige RN-04 no ocurre
    /// aqui sino en el caso de uso, y solo si el aprendiz cambia (H5).
    /// </remarks>
    public async Task ReasignarAsync(int id, int instructorId, int aprendizId, CancellationToken ct)
    {
        var practica = await _contexto.Practicas.FindAsync(new object?[] { id }, ct)
            ?? throw new NoEncontradoException($"No existe una practica con Id {id}.");

        practica.Reasignar(instructorId, aprendizId);
    }

    /// <summary>
    /// Grafo de carga comun a las cuatro consultas de lectura. Las navegaciones
    /// Instructor y Aprendiz son las que agrego H6 justamente para esto: sin
    /// ellas, aplanar el nombre de cada participante costaria una consulta
    /// adicional por practica.
    /// </summary>
    /// <remarks>
    /// Programa entra por ThenInclude porque la practica no guarda programa_id:
    /// se deriva via ficha_id, tal como anota el Script_DDL.sql para mantener la
    /// tercera forma normal. De ahi salen tanto el filtro programaId de H19 como
    /// los programas derivados del Instructor en GET /api/programas (H20).
    /// </remarks>
    private static IQueryable<Practica> ConGrafoCompleto(IQueryable<Practica> consulta) =>
        consulta
            .Include(p => p.Ficha).ThenInclude(f => f.Programa)
            .Include(p => p.Empresa)
            .Include(p => p.Instructor)
            .Include(p => p.Aprendiz);
}
