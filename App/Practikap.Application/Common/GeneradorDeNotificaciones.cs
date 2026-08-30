using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.Common;

/// <summary>
/// Unica implementacion de <see cref="IGeneradorDeNotificaciones"/> (L6).
/// </summary>
/// <remarks>
/// Depende de INotificacionRepository y de nada mas. En particular no depende de
/// IUnidadDeTrabajo, y esa ausencia es deliberada: es lo que hace imposible que
/// esta clase confirme por su cuenta y lo que deja la notificacion dentro de la
/// transaccion del evento que la origino (ADR-02).
///
/// Vive en la capa de Aplicacion y no en Infraestructura porque no toca EF Core
/// ni ningun detalle de persistencia: habla contra un contrato del Dominio, igual
/// que cualquier caso de uso. Se registra a mano en AddApplication, con el mismo
/// criterio con el que ADR-05 enumera los casos de uso: es un servicio con
/// alcance Scoped que comparte el DbContext de la peticion.
///
/// No es una clase estatica como AccesoALaPractica o ParticipantesDePractica,
/// aunque tampoco tenga estado propio: aquellas reciben sus colaboradores por
/// parametro y esta tiene una dependencia fija que conviene inyectar una vez y no
/// arrastrar por la firma de los cuatro puntos de enganche.
///
/// Los textos son afirmaciones sobre el hecho, en tercera persona y sin nombrar
/// al emisor. Nombrarlo obligaria a cargar un Usuario solo para redactar, que es
/// justo lo que H32 vigila.
/// </remarks>
internal sealed class GeneradorDeNotificaciones : IGeneradorDeNotificaciones
{
    private readonly INotificacionRepository _notificacionRepo;

    /// <summary>Crea el generador.</summary>
    /// <param name="notificacionRepo">Acceso a las notificaciones.</param>
    public GeneradorDeNotificaciones(INotificacionRepository notificacionRepo) =>
        _notificacionRepo = notificacionRepo;

    /// <inheritdoc />
    public Task PorObservacionAsync(int destinatarioId, int practicaId, CancellationToken ct) =>
        RegistrarAsync(
            destinatarioId,
            TipoNotificacion.Observacion,
            $"Su instructor registro una observacion nueva en el seguimiento de la practica {practicaId}.",
            ct);

    /// <inheritdoc />
    public Task PorCalificacionAsync(int destinatarioId, int practicaId, CancellationToken ct) =>
        RegistrarAsync(
            destinatarioId,
            TipoNotificacion.Calificacion,
            $"Se registro una calificacion nueva sobre la practica {practicaId}.",
            ct);

    /// <inheritdoc />
    public Task PorMensajeAsync(int destinatarioId, int practicaId, CancellationToken ct) =>
        RegistrarAsync(
            destinatarioId,
            TipoNotificacion.Mensaje,
            $"Recibio un mensaje nuevo en la practica {practicaId}.",
            ct);

    /// <inheritdoc />
    public async Task<Notificacion> AdministrativaAsync(
        int destinatarioId, string contenido, CancellationToken ct) =>
        await RegistrarAsync(destinatarioId, TipoNotificacion.Administrativa, contenido, ct);

    /// <summary>
    /// Construye la notificacion y la registra, sin confirmar. Es el unico punto
    /// del sistema donde se instancia una <see cref="Notificacion"/> que no viene
    /// del Motor.
    /// </summary>
    /// <param name="destinatarioId">Usuario al que va dirigida.</param>
    /// <param name="tipo">Origen funcional del aviso.</param>
    /// <param name="contenido">Texto que vera el destinatario.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La notificacion registrada, con Id todavia en 0.</returns>
    /// <exception cref="Domain.Exceptions.ReglaDeDominioException">
    /// Si el destinatario o el contenido son invalidos. Es la guarda del
    /// constructor de la entidad, que aqui actua como red: los tres eventos
    /// derivan el destinatario de una practica ya cargada y el texto de una
    /// plantilla, y el camino administrativo pasa antes por su validador.
    /// </exception>
    private async Task<Notificacion> RegistrarAsync(
        int destinatarioId, TipoNotificacion tipo, string contenido, CancellationToken ct)
    {
        var notificacion = new Notificacion(destinatarioId, tipo, contenido);

        await _notificacionRepo.AgregarAsync(notificacion, ct);

        // Sin GuardarCambiosAsync, y sin manera de invocarlo. Confirma el caso de
        // uso que produjo el evento, en una sola transaccion con el (L6, ADR-02).
        return notificacion;
    }
}
