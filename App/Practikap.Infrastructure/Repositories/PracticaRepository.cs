using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
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
    /// Es la unica consulta de este repositorio que no pasa por ConGrafoCompleto y
    /// la unica de lectura que no lleva AsNoTracking. Las dos ausencias son el
    /// motivo por el que el metodo existe, y estan justificadas en el contrato.
    ///
    /// Los identificadores se materializan antes de la consulta porque Contains
    /// sobre un IEnumerable diferido se traduciria a una expresion que EF Core no
    /// puede parametrizar. La lista vacia corta sin ir a la base: un IN () no es
    /// SQL valido en MySQL.
    ///
    /// Se materializan como List y no como array, y eso <b>no es indiferente</b>.
    /// Sobre un int[], la resolucion de sobrecargas de C# 14 elige
    /// MemoryExtensions.Contains(ReadOnlySpan&lt;int&gt;, int) en lugar de
    /// Enumerable.Contains, y el arbol de expresion no puede compilar un
    /// ReadOnlySpan como argumento generico: la consulta falla en tiempo de
    /// ejecucion con un TypeLoadException que no menciona nada de esto. List no
    /// tiene esa sobrecarga y la traduccion sale como el IN esperado.
    /// </remarks>
    public async Task<IReadOnlyList<Practica>> ListarPorIdsAsync(
        IEnumerable<int> ids, CancellationToken ct)
    {
        var identificadores = ids.Distinct().ToList();

        if (identificadores.Count == 0)
            return [];

        return await _contexto.Practicas
            .Where(p => identificadores.Contains(p.Id))
            .ToListAsync(ct);
    }

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
    /// El caso de uso tipico obtiene la practica con ObtenerPorIdAsync (queda
    /// rastreada), la modifica con sus metodos de dominio y EF Core detecta el
    /// cambio sin llamada adicional. Este metodo solo actua cuando la practica
    /// llega desatada, para no depender de que el llamador siempre use la
    /// instancia rastreada. Mismo criterio que UsuarioRepository.ActualizarAsync.
    ///
    /// H28 lo puso en lugar de ActualizarEstadoAsync y ReasignarAsync, que
    /// invocaban Practica.CambiarEstado y Practica.Reasignar desde dentro del
    /// repositorio. El primero ademas cableaba esAdministrador en true, lo que
    /// metia una decision de autorizacion en una capa que no conoce
    /// IContextoUsuario y solo era correcto mientras H17 siguiera vigente.
    /// </remarks>
    public Task ActualizarAsync(Practica practica, CancellationToken ct)
    {
        if (_contexto.Entry(practica).State == EntityState.Detached)
            _contexto.Practicas.Update(practica);

        return Task.CompletedTask;
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
