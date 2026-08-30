using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Mensajes;
using Practikap.Application.UseCases.Seguimientos;
using Practikap.Domain.Entities;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Mensajes;

/// <summary>
/// Envia un mensaje entre los dos participantes de una practica (RF-07, CU-06,
/// K2, K3).
/// </summary>
/// <remarks>
/// El cuerpo de la peticion trae la practica y el contenido, nada mas. El emisor
/// sale de IContextoUsuario y el receptor se deriva como el otro participante de
/// la practica que la puerta de escritura ya cargo. Esa derivacion es K2, y es lo
/// que hace que no exista forma de escribirle a un usuario con el que no se
/// comparte practica: no hay campo donde nombrarlo.
///
/// La notificacion que RF-07 describe no se genera aqui. El modulo M6 se reparte
/// entre dos pasos (Doc_Arquitectura 7.1) y las notificaciones son el 4.6; el
/// punto de enganche queda marcado mas abajo, con la misma forma que los de
/// RN-06 en M5.
///
/// Solo el Instructor y el Aprendiz envian. El Administrador lee con alcance de
/// supervision pero no escribe (K4), y queda fuera por la puerta de
/// AccesoALaPractica: no es participante de ninguna practica.
/// </remarks>
public sealed class EnviarMensajeUseCase
{
    private readonly IMensajeRepository _mensajeRepo;
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<EnviarMensajeRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<EnviarMensajeUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="mensajeRepo">Acceso a los mensajes.</param>
    /// <param name="practicaRepo">Acceso a practicas, para las puertas de K3 y de participacion.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public EnviarMensajeUseCase(
        IMensajeRepository mensajeRepo,
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<EnviarMensajeRequest> validador,
        IMapper mapeador,
        ILogger<EnviarMensajeUseCase> registro)
    {
        _mensajeRepo = mensajeRepo;
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Envia el mensaje y devuelve sus datos.</summary>
    /// <param name="request">Practica y contenido.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El mensaje enviado, con la fecha que fijo el servidor.</returns>
    /// <exception cref="ValidationException">
    /// Si el DTO no supera la validacion de forma, incluido el contenido vacio o
    /// mas largo que el tope de K10 (400).
    /// </exception>
    /// <exception cref="AutorizacionException">
    /// Si el solicitante no participa de la practica, o si no comparte con el
    /// receptor ninguna practica activa (403).
    /// </exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si la practica no existe, o si no esta En curso ni En riesgo (422, K3).
    /// </exception>
    public async Task<MensajeResponse> ExecuteAsync(
        EnviarMensajeRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        var practica = await AccesoALaPractica.VerificarEscrituraDeParticipanteAsync(
            _practicaRepo, _contexto, request.PracticaId, ct);

        var emisorId = _contexto.UsuarioId;

        // K2. La practica tiene exactamente dos participantes y el emisor es uno
        // de los dos —eso lo acaba de garantizar la puerta de arriba—, de modo
        // que el receptor es el otro. No hay tercera posibilidad que contemplar.
        var receptorId = practica.InstructorId == emisorId
            ? practica.AprendizId
            : practica.InstructorId;

        // La guarda que K2 pide y que CU-06 nombra para su 403. Es redundante
        // despues de la puerta de arriba: quien participa de una practica En
        // curso o En riesgo comparte por definicion una practica sin finalizar
        // con el otro participante. Se invoca igual, porque es el hecho que el
        // contrato del Doc_Arquitectura 6.6 declara para esta regla y porque deja
        // el rechazo escrito donde CU-06 lo describe, en lugar de hacerlo
        // depender de un razonamiento sobre otra comprobacion.
        if (!await _mensajeRepo.CompartenPracticaActivaAsync(emisorId, receptorId, ct))
            throw new AutorizacionException(
                "Solo puede intercambiar mensajes con quien comparte una practica activa.");

        var mensaje = new Mensaje(request.PracticaId, emisorId, receptorId, request.Contenido);

        await _mensajeRepo.AgregarAsync(mensaje, ct);

        // Hasta aqui mensaje.Id vale 0 y FechaEnvio es el valor por defecto de
        // DateTime. La confirmacion asigna el primero y trae de vuelta la
        // segunda, que es la que escribio MySQL.
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        // Punto de enganche de la notificacion de RF-07 (K7). CU-06 pide que el
        // sistema genere una notificacion interna al recibirse un mensaje, con
        // tipo 'Mensaje' en la tabla notificaciones. Las notificaciones son el
        // paso 4.6: aqui no se implementan ni se simulan. Cuando lleguen,
        // consumiran INotificacionRepository.AgregarAsync sobre este receptorId.

        _registro.LogInformation(
            "Mensaje {MensajeId} enviado por el usuario {EmisorId} al usuario {ReceptorId} en la practica {PracticaId}.",
            mensaje.Id, emisorId, receptorId, mensaje.PracticaId);

        return _mapeador.Map<MensajeResponse>(mensaje);
    }
}
