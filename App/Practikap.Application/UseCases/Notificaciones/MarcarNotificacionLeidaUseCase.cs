using AutoMapper;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Notificaciones;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Notificaciones;

/// <summary>
/// Marca una notificacion como leida por su destinatario (RF-07, CU-06, L1, L4).
/// </summary>
/// <remarks>
/// La puerta es la identidad del destinatario, no el rol, y por eso el controlador
/// no lleva restriccion por rol en este endpoint: la resuelve este caso de uso
/// sobre IContextoUsuario (ADR-03). Quien no sea el destinatario recibe 403, y eso
/// incluye al Administrador que la emitio: emitirla no le da derecho a darla por
/// leida en nombre de otro.
///
/// El segundo PATCH sobre la misma notificacion devuelve 200 y no 422 (L4), con el
/// mismo criterio de K9 y por la misma razon de fondo: Notificacion.MarcarLeida
/// viene sin guarda desde el paso 3.1 y asi se queda. Diverge a proposito de las
/// anulaciones de M4 y M5, que son irreversibles y atribuyen la marca a un
/// Administrador, de modo que repetirlas es un error que vale la pena rechazar.
/// Marcar como leido lo que ya se leyo es la misma intencion repetida.
///
/// CU-06 describe que las notificaciones se marcan al abrirlas. La Matriz_de_Roles
/// hoja 3 no lista este endpoint: la divergencia queda como FA-32 documental.
/// </remarks>
public sealed class MarcarNotificacionLeidaUseCase
{
    private readonly INotificacionRepository _notificacionRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IMapper _mapeador;
    private readonly ILogger<MarcarNotificacionLeidaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="notificacionRepo">Acceso a las notificaciones.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public MarcarNotificacionLeidaUseCase(
        INotificacionRepository notificacionRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IMapper mapeador,
        ILogger<MarcarNotificacionLeidaUseCase> registro)
    {
        _notificacionRepo = notificacionRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Aplica la marca de lectura.</summary>
    /// <param name="id">Notificacion a marcar.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La notificacion, ya con su marca.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no es el destinatario (403).</exception>
    /// <exception cref="NoEncontradoException">Si la notificacion no existe (404).</exception>
    public async Task<NotificacionResponse> ExecuteAsync(int id, CancellationToken ct)
    {
        var notificacion = await _notificacionRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Notificacion", id);

        // L4. Se comprueba despues de cargar la notificacion y no antes, porque la
        // pregunta es sobre esta notificacion en concreto y no sobre el rol: no
        // hay atributo del controlador que pudiera responderla.
        if (notificacion.UsuarioId != _contexto.UsuarioId)
            throw new AutorizacionException(
                "Solo el destinatario puede marcar una notificacion como leida.");

        notificacion.MarcarLeida();

        await _notificacionRepo.ActualizarAsync(notificacion, ct);
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Notificacion {NotificacionId} marcada como leida por su destinatario {DestinatarioId}.",
            notificacion.Id, _contexto.UsuarioId);

        return _mapeador.Map<NotificacionResponse>(notificacion);
    }
}
