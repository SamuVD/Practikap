using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="IMensajeRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M6, mensajeria interna.
/// </summary>
/// <remarks>
/// No invoca dominio, con el criterio de H28, I9 y J7. El contrato que dejo el
/// paso 3.1 declaraba un MarcarLeidoAsync que recibia el identificador y habria
/// obligado a este archivo a cargar el mensaje y llamarle MarcarLeido; se
/// reemplazo por ObtenerPorIdAsync y ActualizarAsync, y la marca la aplica el
/// caso de uso.
///
/// Tampoco decide alcance: los tres de RN-13 los resuelve el caso de uso sobre
/// IContextoUsuario (ADR-03), ni siquiera en CompartenPracticaActivaAsync, que
/// responde un hecho y no una autorizacion.
///
/// Ninguna consulta lleva Include. Mensaje.Practica es la unica propiedad de
/// navegacion de la entidad y no la lee nadie: el caso de uso que necesita la
/// practica la carga por su cuenta contra IPracticaRepository. Emisor y receptor
/// son claves foraneas desnudas, de modo que ningun Usuario entra jamas en el
/// grafo y H32 no tiene por donde romperse.
/// </remarks>
internal sealed class MensajeRepository : IMensajeRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public MensajeRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// Se rastrea, sin AsNoTracking: es la consulta que alimenta el PATCH de
    /// lectura, que modifica el mensaje obtenido y confia en que EF Core detecte
    /// el cambio. Mismo criterio que ObservacionRepository.ObtenerPorIdAsync.
    /// </remarks>
    public Task<Mensaje?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        _contexto.Mensajes
            .FirstOrDefaultAsync(mensaje => mensaje.Id == id, ct);

    /// <inheritdoc />
    /// <remarks>
    /// Orden ascendente, al reves que los listados de M4 y M5. Aquellos son
    /// historiales de auditoria y se leen del ultimo hacia atras; este es un hilo
    /// de conversacion y se lee en el orden en que se escribio.
    ///
    /// ThenBy(Id) como desempate: fecha_envio es DATETIME y tiene precision de
    /// segundo, de modo que dos mensajes seguidos pueden compartir marca.
    ///
    /// No filtra por emisor ni por receptor. Todos los mensajes de una practica
    /// pertenecen a sus dos participantes, asi que quien ya paso la puerta de
    /// RN-13 sobre la practica puede ver el hilo entero.
    /// </remarks>
    public async Task<IReadOnlyList<Mensaje>> ListarPorPracticaAsync(
        int practicaId, CancellationToken ct) =>
        await _contexto.Mensajes
            .AsNoTracking()
            .Where(mensaje => mensaje.PracticaId == practicaId)
            .OrderBy(mensaje => mensaje.FechaEnvio)
            .ThenBy(mensaje => mensaje.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02): el Id que devuelve solo queda poblado despues de que
    /// el caso de uso llame IUnidadDeTrabajo.GuardarCambiosAsync.
    ///
    /// Tampoco escribe FechaEnvio. La columna esta mapeada como generada por la
    /// base con DEFAULT CURRENT_TIMESTAMP, de modo que la marca de tiempo la pone
    /// MySQL y no hay ninguna linea de C# que pudiera sustituirla por la del
    /// cliente.
    /// </remarks>
    public Task<int> AgregarAsync(Mensaje mensaje, CancellationToken ct)
    {
        _contexto.Mensajes.Add(mensaje);
        return Task.FromResult(mensaje.Id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// El caso de uso tipico obtiene el mensaje con ObtenerPorIdAsync (queda
    /// rastreado), le aplica MarcarLeido y EF Core detecta el cambio sin llamada
    /// adicional. Este metodo solo actua cuando la entidad llega desatada, para no
    /// depender de que el llamador siempre use la instancia rastreada. Mismo
    /// criterio que ObservacionRepository.ActualizarAsync.
    /// </remarks>
    public Task ActualizarAsync(Mensaje mensaje, CancellationToken ct)
    {
        if (_contexto.Entry(mensaje).State == EntityState.Detached)
            _contexto.Mensajes.Update(mensaje);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Consulta practicas y no mensajes, que es lo que el hecho pide: la pregunta
    /// es si los dos usuarios tienen vinculo, no si ya se escribieron.
    ///
    /// El par se busca en los dos ordenes porque el contrato no dice cual de los
    /// dos identificadores es el instructor. Quien llama sabe quien emite, pero
    /// eso no determina que rol ocupa en la practica.
    ///
    /// "Activa" es Estado != Finalizada, que es la definicion que ya fijo
    /// PracticaRepository.TieneActivaAsync y la que describe el comentario del
    /// contrato. Es mas amplia que la puerta de K3, que ademas exige En curso o
    /// En riesgo para enviar: esta responde si existe vinculo, y aquella si el
    /// momento admite escritura.
    ///
    /// Compara contra el enumerado y no contra Practica.EstaActiva, que esta
    /// marcada como Ignore en PracticaConfiguration y no tiene traduccion a SQL.
    /// </remarks>
    public Task<bool> CompartenPracticaActivaAsync(
        int emisorId, int receptorId, CancellationToken ct) =>
        _contexto.Practicas
            .AsNoTracking()
            .AnyAsync(practica =>
                practica.Estado != EstadoPractica.Finalizada
                && ((practica.InstructorId == emisorId && practica.AprendizId == receptorId)
                    || (practica.InstructorId == receptorId && practica.AprendizId == emisorId)), ct);
}
