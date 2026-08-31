using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="ICalificacionInstructorRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M5, direccion Instructor hacia
/// Aprendiz.
/// </summary>
/// <remarks>
/// El repositorio no invoca dominio (J7, que extiende H28 e I9 a M5): no hay un
/// AnularAsync que cargue la entidad y le aplique la marca. El caso de uso carga
/// con ObtenerPorIdAsync, que es rastreado, invoca CalificacionInstructor.Anular
/// y confirma.
///
/// Tampoco decide alcance: los tres de RN-13 los resuelve el caso de uso sobre
/// IContextoUsuario (ADR-03). Y nunca consulta la tabla de la direccion
/// contraria, que es RN-10.
///
/// Ninguna consulta lleva Include, a diferencia de las de M4. La practica no hace
/// falta en el grafo porque el unico caso de uso que necesita resolver alcance ya
/// la carga por su cuenta contra IPracticaRepository, y el anulador no tiene
/// propiedad de navegacion que incluir. El resultado es que ningun Usuario entra
/// nunca en el grafo y H32 no tiene por donde romperse.
/// </remarks>
internal sealed class CalificacionInstructorRepository : ICalificacionInstructorRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public CalificacionInstructorRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// Se rastrea, sin AsNoTracking: es la consulta que alimenta el PATCH de
    /// anulacion, que modifica la calificacion obtenida y confia en que EF Core
    /// detecte el cambio. Mismo criterio que SeguimientoRepository.ObtenerPorIdAsync.
    /// </remarks>
    public Task<CalificacionInstructor?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        _contexto.CalificacionesInstructor
            .FirstOrDefaultAsync(calificacion => calificacion.Id == id, ct);

    /// <inheritdoc />
    /// <remarks>
    /// No filtra las anuladas, con el mismo criterio que I4 fijo para el historial
    /// de M4: un registro anulado sigue formando parte de la serie y sale con su
    /// marca y con el identificador de quien la puso. Lo que si lo excluye es el
    /// promedio vigente, que es donde la anulacion tiene efecto (J5).
    ///
    /// El orden es descendente por fecha, con el identificador como desempate:
    /// dos calificaciones de la misma practica registradas dentro del mismo
    /// segundo comparten fecha_registro, que tiene precision de segundo.
    /// </remarks>
    public async Task<IReadOnlyList<CalificacionInstructor>> ListarPorPracticaAsync(
        int practicaId, CancellationToken ct) =>
        await _contexto.CalificacionesInstructor
            .AsNoTracking()
            .Where(calificacion => calificacion.PracticaId == practicaId)
            .OrderByDescending(calificacion => calificacion.FechaRegistro)
            .ThenByDescending(calificacion => calificacion.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// Excluye las anuladas (J5). Un registro que el Administrador anulo no puede
    /// seguir pesando en el promedio: si contara, la anulacion no tendria ningun
    /// efecto observable y el umbral de RN-09 se evaluaria sobre datos que el
    /// Administrador ya declaro invalidos.
    ///
    /// La proyeccion a decimal? antes del promedio no es cosmetica: AverageAsync
    /// sobre una secuencia vacia lanza InvalidOperationException, y una practica
    /// sin calificar es el caso normal, no el excepcional. Con la proyeccion
    /// devuelve null y el contrato pide cero.
    ///
    /// El redondeo a dos decimales acota el resultado de dividir valores
    /// DECIMAL(3,1), que de otro modo arrastraria una cola de decimales sin
    /// significado.
    /// </remarks>
    public async Task<decimal> PromedioVigenteAsync(int practicaId, CancellationToken ct)
    {
        var promedio = await _contexto.CalificacionesInstructor
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
    /// PromedioVigenteAsync una vez por practica: un reporte grupal de treinta
    /// practicas costaria treinta consultas.
    ///
    /// El redondeo se aplica en memoria y no en la consulta, igual que en
    /// PromedioVigenteAsync, para que las dos vias produzcan exactamente el mismo
    /// numero. Redondear en SQL dejaria la definicion de promedio vigente en dos
    /// dialectos distintos.
    ///
    /// Aqui no hace falta la proyeccion a decimal? que PromedioVigenteAsync si
    /// necesita: aquella promedia una secuencia que puede venir vacia, y un
    /// GROUP BY no devuelve grupos vacios. La practica sin calificaciones
    /// computables simplemente no produce fila y queda fuera del diccionario.
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

        var promedios = await _contexto.CalificacionesInstructor
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
    /// No confirma (ADR-02): el Id que devuelve solo queda poblado despues de que
    /// el caso de uso llame IUnidadDeTrabajo.GuardarCambiosAsync.
    ///
    /// Tampoco escribe FechaRegistro. La columna esta mapeada como generada por la
    /// base con DEFAULT CURRENT_TIMESTAMP, de modo que la marca de tiempo la pone
    /// MySQL y no hay ninguna linea de C# que pudiera sustituirla por la del
    /// cliente. Eso es RN-11.
    /// </remarks>
    public Task<int> AgregarAsync(CalificacionInstructor calificacion, CancellationToken ct)
    {
        _contexto.CalificacionesInstructor.Add(calificacion);
        return Task.FromResult(calificacion.Id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// El caso de uso tipico obtiene la calificacion con ObtenerPorIdAsync (queda
    /// rastreada), le aplica Anular y EF Core detecta el cambio sin llamada
    /// adicional. Este metodo solo actua cuando la entidad llega desatada, para no
    /// depender de que el llamador siempre use la instancia rastreada. Mismo
    /// criterio que SeguimientoRepository.ActualizarAsync.
    /// </remarks>
    public Task ActualizarAsync(CalificacionInstructor calificacion, CancellationToken ct)
    {
        if (_contexto.Entry(calificacion).State == EntityState.Detached)
            _contexto.CalificacionesInstructor.Update(calificacion);

        return Task.CompletedTask;
    }
}
