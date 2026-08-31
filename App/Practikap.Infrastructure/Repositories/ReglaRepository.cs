using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="IReglaRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M2, Motor de Reglas.
/// </summary>
/// <remarks>
/// No invoca dominio, con el criterio de H28, I9, J7, L8 y N8. El contrato que
/// dejo el paso 3.1 declaraba un CambiarEstadoAsync que recibia el identificador y
/// habria obligado a este archivo a cargar la regla y llamarle Activar o
/// Desactivar; se elimino, y la activacion la aplica el caso de uso sobre la
/// instancia que devuelve ObtenerPorIdAsync.
///
/// Tampoco evalua. Este archivo carga reglas y nada mas: quien decide cual se
/// aplica es MotorDeReglas, en el Dominio, que no conoce EF Core (ADR-04). La
/// division es la razon de que ListarActivasOrdenadasAsync devuelva la coleccion
/// entera en lugar de una consulta que ya filtre por la condicion.
///
/// Ninguna consulta lleva Include. Regla.Creador es la unica propiedad de
/// navegacion de la entidad y no la lee nadie: ReglaResponse expone CreadoPor
/// desnudo, de modo que ningun Usuario entra en el grafo y H32 no tiene por donde
/// romperse (RNF-05).
/// </remarks>
internal sealed class ReglaRepository : IReglaRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public ReglaRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// Es la consulta que alimenta al Motor, y el orden que emite es el contrato
    /// de RN-07: el evaluador aplica la primera coincidencia del orden recibido y
    /// no vuelve a ordenar.
    ///
    /// ThenBy(Id) como desempate. prioridad no es unica —el DDL la declara
    /// INT UNSIGNED DEFAULT 0, sin indice unico—, de modo que sin este segundo
    /// criterio dos reglas de igual prioridad se aplicarian segun lo que decidiera
    /// el plan de ejecucion de MySQL, y RN-07 exige un resultado determinista y
    /// reproducible. El desempate por Id da la regla mas antigua, que es la que ya
    /// venia rigiendo.
    ///
    /// El WHERE y el ORDER BY los sostiene idx_reglas_activa_prioridad, que la
    /// migracion InicialPractikap creo justo para esta consulta.
    /// </remarks>
    public async Task<IReadOnlyList<Regla>> ListarActivasOrdenadasAsync(CancellationToken ct) =>
        await _contexto.Reglas
            .AsNoTracking()
            .Where(regla => regla.Activa)
            .OrderBy(regla => regla.Prioridad)
            .ThenBy(regla => regla.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// Sin filtro por Activa: es el panel del Administrador, que necesita ver las
    /// reglas retiradas para poder volver a activarlas (RN-08). Se conserva el
    /// mismo orden que la consulta del Motor para que el panel muestre las reglas
    /// en el orden en que se aplicarian.
    /// </remarks>
    public async Task<IReadOnlyList<Regla>> ListarAsync(CancellationToken ct) =>
        await _contexto.Reglas
            .AsNoTracking()
            .OrderBy(regla => regla.Prioridad)
            .ThenBy(regla => regla.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// Se rastrea, sin AsNoTracking: es la consulta que alimenta al PUT y al PATCH
    /// de activacion, que modifican la regla obtenida y confian en que EF Core
    /// detecte el cambio. Mismo criterio que NotificacionRepository.ObtenerPorIdAsync.
    /// </remarks>
    public Task<Regla?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        _contexto.Reglas
            .FirstOrDefaultAsync(regla => regla.Id == id, ct);

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02): el Id que devuelve solo queda poblado despues de que
    /// el caso de uso llame IUnidadDeTrabajo.GuardarCambiosAsync.
    ///
    /// Tampoco escribe FechaCreacion ni FechaActualizacion. Las dos columnas estan
    /// mapeadas como generadas por la base con DEFAULT CURRENT_TIMESTAMP, de modo
    /// que las marcas de tiempo las pone MySQL.
    /// </remarks>
    public Task<int> AgregarAsync(Regla regla, CancellationToken ct)
    {
        _contexto.Reglas.Add(regla);
        return Task.FromResult(regla.Id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// El caso de uso tipico obtiene la regla con ObtenerPorIdAsync (queda
    /// rastreada), le aplica Actualizar, Activar o Desactivar y EF Core detecta el
    /// cambio sin llamada adicional. Este metodo solo actua cuando la entidad llega
    /// desatada, para no depender de que el llamador siempre use la instancia
    /// rastreada. Mismo criterio que NotificacionRepository.ActualizarAsync.
    /// </remarks>
    public Task ActualizarAsync(Regla regla, CancellationToken ct)
    {
        if (_contexto.Entry(regla).State == EntityState.Detached)
            _contexto.Reglas.Update(regla);

        return Task.CompletedTask;
    }
}
