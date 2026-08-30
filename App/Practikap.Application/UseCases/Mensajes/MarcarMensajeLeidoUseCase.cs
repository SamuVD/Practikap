using AutoMapper;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Mensajes;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Mensajes;

/// <summary>
/// Marca un mensaje como leido por su destinatario (RF-07, K5, K6).
/// </summary>
/// <remarks>
/// La puerta es la identidad del receptor, no el rol, y por eso el controlador no
/// lleva restriccion por rol en este endpoint: la resuelve este caso de uso sobre
/// IContextoUsuario (ADR-03). Quien no sea el destinatario recibe 403, y eso
/// incluye al emisor —que no marca como leido lo que el mismo escribio— y al
/// Administrador, que lee los hilos pero no es receptor de ninguno (K4, K5).
///
/// No comprueba la practica ni su estado. La marca de lectura es un hecho sobre
/// el mensaje, no un registro nuevo sobre la practica: se puede leer un mensaje
/// de una practica ya Finalizada, del mismo modo que se puede consultar su hilo
/// (K3).
///
/// El segundo PATCH sobre el mismo mensaje devuelve 200 y no 422 (K9). Diverge a
/// proposito de las anulaciones de M4 y M5: aquellas son irreversibles y
/// atribuyen la marca a un Administrador, de modo que repetirlas es un error que
/// vale la pena rechazar. Marcar como leido lo que ya se leyo es la misma
/// intencion repetida, y Mensaje.MarcarLeido viene sin guarda desde el paso 3.1.
/// </remarks>
public sealed class MarcarMensajeLeidoUseCase
{
    private readonly IMensajeRepository _mensajeRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IMapper _mapeador;
    private readonly ILogger<MarcarMensajeLeidoUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="mensajeRepo">Acceso a los mensajes.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public MarcarMensajeLeidoUseCase(
        IMensajeRepository mensajeRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IMapper mapeador,
        ILogger<MarcarMensajeLeidoUseCase> registro)
    {
        _mensajeRepo = mensajeRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Aplica la marca de lectura.</summary>
    /// <param name="id">Mensaje a marcar.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El mensaje, ya con su marca.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no es el destinatario (403).</exception>
    /// <exception cref="NoEncontradoException">Si el mensaje no existe (404).</exception>
    public async Task<MensajeResponse> ExecuteAsync(int id, CancellationToken ct)
    {
        var mensaje = await _mensajeRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Mensaje", id);

        // K5. Se comprueba despues de cargar el mensaje y no antes, porque la
        // pregunta es sobre este mensaje en concreto y no sobre el rol: no hay
        // atributo del controlador que pudiera responderla.
        if (mensaje.ReceptorId != _contexto.UsuarioId)
            throw new AutorizacionException(
                "Solo el destinatario puede marcar un mensaje como leido.");

        mensaje.MarcarLeido();

        await _mensajeRepo.ActualizarAsync(mensaje, ct);
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Mensaje {MensajeId} marcado como leido por su destinatario {ReceptorId}.",
            mensaje.Id, _contexto.UsuarioId);

        return _mapeador.Map<MensajeResponse>(mensaje);
    }
}
