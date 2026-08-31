using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="ICalificacionAprendizRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M5, direccion Aprendiz hacia
/// Instructor.
/// </summary>
/// <remarks>
/// Gemelo de <see cref="CalificacionInstructorRepository"/> sobre la otra tabla.
/// La simetria es literal y esta buscada: RN-10 exige dos caminos independientes,
/// y factorizar los dos repositorios en una base generica comun habria creado el
/// acoplamiento que la regla prohibe, ademas de ir contra las dos tablas
/// separadas que CU-05 y HU-07 pidieron explicitamente.
///
/// Valen aqui las mismas notas que alla: no invoca dominio (J7), no decide
/// alcance (ADR-03), no consulta la direccion contraria (RN-10) y ninguna
/// consulta lleva Include, de modo que ningun Usuario entra en el grafo (H32).
/// </remarks>
internal sealed class CalificacionAprendizRepository : ICalificacionAprendizRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public CalificacionAprendizRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// Se rastrea, sin AsNoTracking: alimenta el PATCH de anulacion.
    /// </remarks>
    public Task<CalificacionAprendiz?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        _contexto.CalificacionesAprendiz
            .FirstOrDefaultAsync(calificacion => calificacion.Id == id, ct);

    /// <inheritdoc />
    /// <remarks>
    /// No filtra las anuladas (I4 aplicado a M5): salen con su marca. Orden
    /// descendente por fecha con el identificador como desempate, porque
    /// fecha_registro tiene precision de segundo.
    /// </remarks>
    public async Task<IReadOnlyList<CalificacionAprendiz>> ListarPorPracticaAsync(
        int practicaId, CancellationToken ct) =>
        await _contexto.CalificacionesAprendiz
            .AsNoTracking()
            .Where(calificacion => calificacion.PracticaId == practicaId)
            .OrderByDescending(calificacion => calificacion.FechaRegistro)
            .ThenByDescending(calificacion => calificacion.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// Excluye las anuladas (J5). La proyeccion a decimal? evita que AverageAsync
    /// lance sobre una practica todavia sin evaluar, que es el caso normal al
    /// principio de una practica, y permite devolver el cero que pide el contrato.
    /// </remarks>
    public async Task<decimal> PromedioVigenteAsync(int practicaId, CancellationToken ct)
    {
        var promedio = await _contexto.CalificacionesAprendiz
            .AsNoTracking()
            .Where(calificacion => calificacion.PracticaId == practicaId && !calificacion.Anulado)
            .Select(calificacion => (decimal?)calificacion.Valor)
            .AverageAsync(ct);

        return promedio is null
            ? decimal.Zero
            : Math.Round(promedio.Value, 2, MidpointRounding.AwayFromZero);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Un unico GROUP BY que MySQL resuelve en el servidor, en lugar de invocar
    /// PromedioVigenteAsync una vez por practica.
    ///
    /// El redondeo se aplica en memoria, igual que en PromedioVigenteAsync, para
    /// que las dos vias produzcan exactamente el mismo numero.
    ///
    /// Aqui no hace falta la proyeccion a decimal? que PromedioVigenteAsync si
    /// necesita: un GROUP BY no devuelve grupos vacios, de modo que la practica
    /// sin calificaciones computables no produce fila y queda fuera del
    /// diccionario.
    ///
    /// Los identificadores se materializan como List y no como array, por el
    /// mismo motivo que documenta PracticaRepository.ListarPorIdsAsync: sobre un
    /// int[], C# 14 elige MemoryExtensions.Contains(ReadOnlySpan&lt;int&gt;, int) y
    /// el arbol de expresion no puede compilar un ReadOnlySpan.
    /// </remarks>
    public async Task<IReadOnlyDictionary<int, decimal>> PromediosPorPracticasAsync(
        IEnumerable<int> practicaIds, CancellationToken ct)
    {
        var identificadores = practicaIds.Distinct().ToList();

        if (identificadores.Count == 0)
            return new Dictionary<int, decimal>();

        var promedios = await _contexto.CalificacionesAprendiz
            .AsNoTracking()
            .Where(calificacion => identificadores.Contains(calificacion.PracticaId)
                                && !calificacion.Anulado)
            .GroupBy(calificacion => calificacion.PracticaId)
            .Select(grupo => new
            {
                PracticaId = grupo.Key,
                Promedio = grupo.Average(calificacion => calificacion.Valor)
            })
            .ToListAsync(ct);

        return promedios.ToDictionary(
            fila => fila.PracticaId,
            fila => Math.Round(fila.Promedio, 2, MidpointRounding.AwayFromZero));
    }

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02) y no escribe FechaRegistro, que la genera MySQL
    /// (RN-11).
    /// </remarks>
    public Task<int> AgregarAsync(CalificacionAprendiz calificacion, CancellationToken ct)
    {
        _contexto.CalificacionesAprendiz.Add(calificacion);
        return Task.FromResult(calificacion.Id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Solo actua cuando la entidad llega desatada. El camino habitual es la
    /// instancia rastreada que devolvio ObtenerPorIdAsync.
    /// </remarks>
    public Task ActualizarAsync(CalificacionAprendiz calificacion, CancellationToken ct)
    {
        if (_contexto.Entry(calificacion).State == EntityState.Detached)
            _contexto.CalificacionesAprendiz.Update(calificacion);

        return Task.CompletedTask;
    }
}
