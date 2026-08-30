using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="IObservacionRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M4.
/// </summary>
/// <remarks>
/// Simetrico a <see cref="SeguimientoRepository"/> y sujeto al mismo criterio de
/// I9: no invoca dominio. La marca de anulacion la aplica Observacion.Anular
/// desde el caso de uso.
/// </remarks>
internal sealed class ObservacionRepository : IObservacionRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public ObservacionRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// Se rastrea, porque alimenta el PATCH de anulacion, y va sin Include: su
    /// unico consumidor es el Administrador, cuya autorizacion sale de
    /// IContextoUsuario y no depende de la practica a la que la observacion
    /// pertenece. Cargar el seguimiento y la practica seria trabajo que nadie lee.
    /// </remarks>
    public Task<Observacion?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        _contexto.Observaciones
            .FirstOrDefaultAsync(observacion => observacion.Id == id, ct);

    /// <inheritdoc />
    /// <remarks>
    /// No filtra los anulados, por el mismo motivo que
    /// SeguimientoRepository.ListarPorPracticaAsync (I4).
    ///
    /// Sin consumidor entre los seis endpoints del modulo: I5 resuelve el
    /// historial anidando las observaciones en el grafo del seguimiento, de modo
    /// que ninguno de ellos necesita listarlas por separado. Se implementa porque
    /// el contrato lo declara y porque es la consulta natural para el detalle de
    /// un seguimiento en la Fase 5.
    /// </remarks>
    public async Task<IReadOnlyList<Observacion>> ListarPorSeguimientoAsync(
        int seguimientoId, CancellationToken ct) =>
        await _contexto.Observaciones
            .AsNoTracking()
            .Where(observacion => observacion.SeguimientoId == seguimientoId)
            .OrderByDescending(observacion => observacion.FechaRegistro)
            .ThenByDescending(observacion => observacion.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02) ni escribe FechaRegistro, que la genera MySQL con
    /// DEFAULT CURRENT_TIMESTAMP (RN-11).
    /// </remarks>
    public Task<int> AgregarAsync(Observacion observacion, CancellationToken ct)
    {
        _contexto.Observaciones.Add(observacion);
        return Task.FromResult(observacion.Id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Solo actua cuando la observacion llega desatada. Mismo criterio que
    /// SeguimientoRepository.ActualizarAsync.
    /// </remarks>
    public Task ActualizarAsync(Observacion observacion, CancellationToken ct)
    {
        if (_contexto.Entry(observacion).State == EntityState.Detached)
            _contexto.Observaciones.Update(observacion);

        return Task.CompletedTask;
    }
}
