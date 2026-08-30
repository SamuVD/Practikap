using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Notificaciones;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Notificaciones;

/// <summary>
/// Emite una notificacion administrativa dirigida a un usuario. Reservado al
/// Administrador (RF-07, L1, L2).
/// </summary>
/// <remarks>
/// Es el unico camino por el que una notificacion nace de una accion deliberada y
/// no de un evento del sistema, y por eso es el unico que lleva tipo
/// Administrativa, el valor que L2 agrego al ENUM.
///
/// El tipo no viaja en el DTO: lo fija GeneradorDeNotificaciones.AdministrativaAsync.
/// Si viajara, el Administrador podria emitir avisos con tipo Mensaje u
/// Observacion sin que el evento hubiera ocurrido, y el destinatario no tendria
/// como distinguirlos de los verdaderos.
///
/// El Administrador emite pero no lee lo que emitio: por L3 su GET devuelve su
/// propia bandeja, no la del destinatario. De ahi que el endpoint responda 201 sin
/// cabecera Location, cosa que su controlador explica.
///
/// A diferencia de los tres eventos de L5, aqui no hay ningun otro registro con el
/// que compartir transaccion: la notificacion es el hecho entero, y este caso de
/// uso es el que confirma.
/// </remarks>
public sealed class CrearNotificacionAdministrativaUseCase
{
    private readonly IGeneradorDeNotificaciones _generador;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CrearNotificacionRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<CrearNotificacionAdministrativaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="generador">Punto unico de emision de notificaciones (L6).</param>
    /// <param name="usuarioRepo">Acceso a usuarios, para comprobar el destinatario.</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CrearNotificacionAdministrativaUseCase(
        IGeneradorDeNotificaciones generador,
        IUsuarioRepository usuarioRepo,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CrearNotificacionRequest> validador,
        IMapper mapeador,
        ILogger<CrearNotificacionAdministrativaUseCase> registro)
    {
        _generador = generador;
        _usuarioRepo = usuarioRepo;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Emite la notificacion y devuelve sus datos.</summary>
    /// <param name="request">Destinatario y contenido.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La notificacion emitida, con la fecha que fijo el servidor.</returns>
    /// <exception cref="ValidationException">
    /// Si el DTO no supera la validacion de forma, incluido el contenido vacio o
    /// mas largo que los 255 caracteres de la columna (400).
    /// </exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si el usuario destinatario no existe (422).
    /// </exception>
    public async Task<NotificacionResponse> ExecuteAsync(
        CrearNotificacionRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        // 422 y no 404, con el mismo criterio de AccesoALaPractica y de
        // ParticipantesDePractica: el identificador viaja en el cuerpo y no en la
        // ruta, asi que no es el recurso pedido lo que falta, es la solicitud lo
        // que no se puede procesar. La comprobacion existe ademas para que un
        // destinatario inexistente no llegue a la clave foranea, donde seria un
        // fallo no controlado en lugar de un rechazo con contrato.
        _ = await _usuarioRepo.ObtenerPorIdAsync(request.UsuarioId, ct)
            ?? throw new ReglaDeDominioException(
                $"El usuario {request.UsuarioId} no existe y no puede recibir notificaciones.",
                "RF-07");

        var notificacion = await _generador.AdministrativaAsync(
            request.UsuarioId, request.Contenido, ct);

        // Hasta aqui notificacion.Id vale 0 y FechaGeneracion es el valor por
        // defecto de DateTime. La confirmacion asigna el primero y trae de vuelta
        // la segunda, que es la que escribio MySQL.
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Notificacion administrativa {NotificacionId} emitida para el usuario {DestinatarioId}.",
            notificacion.Id, notificacion.UsuarioId);

        return _mapeador.Map<NotificacionResponse>(notificacion);
    }
}
