using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="INotificacionRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M6, notificaciones.
/// </summary>
/// <remarks>
/// No invoca dominio, con el criterio de H28, I9, J7 y L8. El contrato que dejo
/// el paso 3.1 declaraba un MarcarLeidaAsync que recibia el identificador y
/// habria obligado a este archivo a cargar la notificacion y llamarle
/// MarcarLeida; se reemplazo por ObtenerPorIdAsync y ActualizarAsync, y la marca
/// la aplica el caso de uso.
///
/// Tampoco decide alcance. Acá el alcance es mas simple que en cualquier otro
/// modulo —cada quien ve solo las suyas, el Administrador incluido (L3)—, pero la
/// identidad sigue llegando desde el caso de uso sobre IContextoUsuario (ADR-03):
/// ListarPorUsuarioAsync recibe un usuarioId y no pregunta de donde salio.
///
/// Ninguna consulta lleva Include. Notificacion.Regla es la unica propiedad de
/// navegacion de la entidad y no la lee nadie: NotificacionResponse expone
/// ReglaId desnudo. El destinatario ni siquiera es navegacion, es una clave
/// foranea sola, de modo que ningun Usuario entra jamas en el grafo y H32 no
/// tiene por donde romperse.
/// </remarks>
internal sealed class NotificacionRepository : INotificacionRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public NotificacionRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// Se rastrea, sin AsNoTracking: es la consulta que alimenta el PATCH de
    /// lectura, que modifica la notificacion obtenida y confia en que EF Core
    /// detecte el cambio. Mismo criterio que MensajeRepository.ObtenerPorIdAsync.
    /// </remarks>
    public Task<Notificacion?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        _contexto.Notificaciones
            .FirstOrDefaultAsync(notificacion => notificacion.Id == id, ct);

    /// <inheritdoc />
    /// <remarks>
    /// Orden descendente, al reves que el hilo de mensajes del paso 4.5. Aquel es
    /// una conversacion y se lee en el orden en que se escribio; esto es una
    /// bandeja de entrada, donde lo ultimo que llego es lo primero que interesa.
    ///
    /// ThenByDescending(Id) como desempate: fecha_generacion es DATETIME y tiene
    /// precision de segundo, de modo que dos notificaciones del mismo evento
    /// pueden compartir marca.
    ///
    /// El filtro de soloNoLeidas se compone sobre el IQueryable y viaja a SQL como
    /// un WHERE mas. No se resuelve en memoria: la bandeja de un usuario crece sin
    /// tope a lo largo de la practica.
    /// </remarks>
    public async Task<IReadOnlyList<Notificacion>> ListarPorUsuarioAsync(
        int usuarioId, bool soloNoLeidas, CancellationToken ct)
    {
        var consulta = _contexto.Notificaciones
            .AsNoTracking()
            .Where(notificacion => notificacion.UsuarioId == usuarioId);

        if (soloNoLeidas)
            consulta = consulta.Where(notificacion => !notificacion.Leida);

        return await consulta
            .OrderByDescending(notificacion => notificacion.FechaGeneracion)
            .ThenByDescending(notificacion => notificacion.Id)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02): el Id que devuelve solo queda poblado despues de que
    /// el caso de uso llame IUnidadDeTrabajo.GuardarCambiosAsync. Es justo esa
    /// ausencia la que permite que la notificacion de un evento se confirme en el
    /// mismo SaveChanges que el evento que la origino (L6).
    ///
    /// Tampoco escribe FechaGeneracion. La columna esta mapeada como generada por
    /// la base con DEFAULT CURRENT_TIMESTAMP, de modo que la marca de tiempo la
    /// pone MySQL.
    /// </remarks>
    public Task<int> AgregarAsync(Notificacion notificacion, CancellationToken ct)
    {
        _contexto.Notificaciones.Add(notificacion);
        return Task.FromResult(notificacion.Id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// El caso de uso tipico obtiene la notificacion con ObtenerPorIdAsync (queda
    /// rastreada), le aplica MarcarLeida y EF Core detecta el cambio sin llamada
    /// adicional. Este metodo solo actua cuando la entidad llega desatada, para no
    /// depender de que el llamador siempre use la instancia rastreada. Mismo
    /// criterio que MensajeRepository.ActualizarAsync.
    /// </remarks>
    public Task ActualizarAsync(Notificacion notificacion, CancellationToken ct)
    {
        if (_contexto.Entry(notificacion).State == EntityState.Detached)
            _contexto.Notificaciones.Update(notificacion);

        return Task.CompletedTask;
    }
}
