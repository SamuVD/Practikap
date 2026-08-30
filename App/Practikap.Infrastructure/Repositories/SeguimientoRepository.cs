using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="ISeguimientoRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M4.
/// </summary>
/// <remarks>
/// El repositorio no invoca dominio (I9, que extiende H28 a M4): no hay un
/// AnularAsync que cargue la entidad y le aplique la marca. El caso de uso carga
/// con ObtenerPorIdAsync, que es rastreado, invoca Seguimiento.Anular y confirma.
///
/// Tampoco decide alcance: los tres de RN-13 los resuelve el caso de uso sobre
/// IContextoUsuario (ADR-03).
/// </remarks>
internal sealed class SeguimientoRepository : ISeguimientoRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public SeguimientoRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// Se rastrea, sin AsNoTracking: es la consulta que alimenta el PATCH de
    /// anulacion, que modifica el seguimiento obtenido y confia en que EF Core
    /// detecte el cambio. Mismo criterio que PracticaRepository.ObtenerPorIdAsync.
    /// </remarks>
    public Task<Seguimiento?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        ConGrafo(_contexto.Seguimientos)
            .FirstOrDefaultAsync(seguimiento => seguimiento.Id == id, ct);

    /// <inheritdoc />
    /// <remarks>
    /// No filtra los anulados (I4): un registro anulado sigue formando parte del
    /// historial y sale con su marca y con el identificador de quien la puso. La
    /// inmutabilidad de RN-12 seria letra muerta si la anulacion equivaliera a
    /// una desaparicion.
    ///
    /// El orden es descendente por fecha, con el identificador como desempate:
    /// dos registros de la misma practica creados dentro del mismo segundo
    /// comparten fecha_registro, que tiene precision de segundo.
    /// </remarks>
    public async Task<IReadOnlyList<Seguimiento>> ListarPorPracticaAsync(
        int practicaId, CancellationToken ct) =>
        await ConGrafo(_contexto.Seguimientos.AsNoTracking())
            .Where(seguimiento => seguimiento.PracticaId == practicaId)
            .OrderByDescending(seguimiento => seguimiento.FechaRegistro)
            .ThenByDescending(seguimiento => seguimiento.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02): el Id que devuelve solo queda poblado despues de
    /// que el caso de uso llame IUnidadDeTrabajo.GuardarCambiosAsync.
    ///
    /// Tampoco escribe FechaRegistro. La columna esta mapeada como generada por
    /// la base con DEFAULT CURRENT_TIMESTAMP, de modo que la marca de tiempo la
    /// pone MySQL y no hay ninguna linea de C# que pudiera sustituirla por la
    /// del cliente. Eso es RN-11.
    /// </remarks>
    public Task<int> AgregarAsync(Seguimiento seguimiento, CancellationToken ct)
    {
        _contexto.Seguimientos.Add(seguimiento);
        return Task.FromResult(seguimiento.Id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// El caso de uso tipico obtiene el seguimiento con ObtenerPorIdAsync (queda
    /// rastreado), le aplica Anular y EF Core detecta el cambio sin llamada
    /// adicional. Este metodo solo actua cuando la entidad llega desatada, para
    /// no depender de que el llamador siempre use la instancia rastreada. Mismo
    /// criterio que UsuarioRepository.ActualizarAsync y PracticaRepository.ActualizarAsync.
    /// </remarks>
    public Task ActualizarAsync(Seguimiento seguimiento, CancellationToken ct)
    {
        if (_contexto.Entry(seguimiento).State == EntityState.Detached)
            _contexto.Seguimientos.Update(seguimiento);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Excluye los anulados. Es insumo del Motor de Reglas para medir
    /// inactividad, y un registro que el Administrador anulo no es actividad: si
    /// contara, anular el ultimo seguimiento de una practica abandonada la
    /// dejaria indefinidamente fuera del alcance de la regla.
    ///
    /// Sin consumidor en la v1. El Motor llega en el paso 4.7.
    /// </remarks>
    public async Task<DateTime?> FechaUltimoRegistroAsync(int practicaId, CancellationToken ct) =>
        await _contexto.Seguimientos
            .AsNoTracking()
            .Where(seguimiento => seguimiento.PracticaId == practicaId && !seguimiento.Anulado)
            .OrderByDescending(seguimiento => seguimiento.FechaRegistro)
            .Select(seguimiento => (DateTime?)seguimiento.FechaRegistro)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Grafo de carga comun a las dos consultas de lectura.
    /// </summary>
    /// <remarks>
    /// Practica es lo que permite resolver los tres alcances de RN-13 sin una
    /// segunda consulta: de ella salen InstructorId y AprendizId, que son los dos
    /// campos que el caso de uso compara contra IContextoUsuario.
    ///
    /// Observaciones es I5: el historial devuelve cada seguimiento con las suyas
    /// anidadas, en una sola consulta y sin recorrer la coleccion por elemento.
    /// La navegacion esta mapeada sobre el campo _observaciones, asi que la carga
    /// no depende de que la propiedad tenga setter.
    ///
    /// El anulador no entra en el grafo, y no por olvido: las dos configuraciones
    /// mapean anulado_por con HasOne&lt;Usuario&gt;().WithMany() sin propiedad de
    /// navegacion, de modo que no hay Usuario que incluir. La respuesta lleva el
    /// identificador desnudo (I4) y la fuga de ContrasenaHash que H32 vigila en
    /// M3 no tiene por donde ocurrir aqui.
    /// </remarks>
    private static IQueryable<Seguimiento> ConGrafo(IQueryable<Seguimiento> consulta) =>
        consulta
            .Include(seguimiento => seguimiento.Practica)
            .Include(seguimiento => seguimiento.Observaciones);
}
