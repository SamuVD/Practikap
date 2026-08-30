using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Observaciones;
using Practikap.Application.UseCases.Seguimientos;
using Practikap.Domain.Entities;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Observaciones;

/// <summary>
/// Registra una observacion sobre un seguimiento. Reservado al Instructor
/// responsable de la practica (I1, I7).
/// </summary>
/// <remarks>
/// Lleva las tres puertas de I10, en este orden: el seguimiento existe (404,
/// porque su identificador si viaja en la ruta), la practica a la que pertenece
/// admite escritura del solicitante (403 o 422, delegado en AccesoALaPractica),
/// y el seguimiento sigue vigente (422).
///
/// La ultima es la que I10 agrega sobre lo que ya exigia el alta de seguimiento:
/// una observacion colgada de un registro anulado nace muerta, porque el
/// historial devolveria una observacion vigente dentro de un seguimiento que ya
/// no cuenta. La comprobacion va al final para no revelar el estado de un
/// seguimiento cuya practica el solicitante no puede tocar.
/// </remarks>
public sealed class RegistrarObservacionUseCase
{
    private readonly IObservacionRepository _observacionRepo;
    private readonly ISeguimientoRepository _seguimientoRepo;
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CrearObservacionRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<RegistrarObservacionUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="observacionRepo">Acceso a observaciones.</param>
    /// <param name="seguimientoRepo">Acceso a seguimientos, para el registro padre.</param>
    /// <param name="practicaRepo">Acceso a practicas, para las puertas de I2 e I7.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public RegistrarObservacionUseCase(
        IObservacionRepository observacionRepo,
        ISeguimientoRepository seguimientoRepo,
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CrearObservacionRequest> validador,
        IMapper mapeador,
        ILogger<RegistrarObservacionUseCase> registro)
    {
        _observacionRepo = observacionRepo;
        _seguimientoRepo = seguimientoRepo;
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Registra la observacion y devuelve sus datos.</summary>
    /// <param name="seguimientoId">Seguimiento al que se asocia.</param>
    /// <param name="request">Contenido de la observacion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La observacion creada, con la fecha que fijo el servidor.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="AutorizacionException">
    /// Si el solicitante no es el instructor responsable de la practica (403, I7).
    /// </exception>
    /// <exception cref="NoEncontradoException">Si el seguimiento no existe (404).</exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si la practica no esta En curso ni En riesgo, o si el seguimiento esta
    /// anulado (422, I2 e I10).
    /// </exception>
    public async Task<ObservacionResponse> ExecuteAsync(
        int seguimientoId, CrearObservacionRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        var seguimiento = await _seguimientoRepo.ObtenerPorIdAsync(seguimientoId, ct)
            ?? throw new NoEncontradoException("Seguimiento", seguimientoId);

        await AccesoALaPractica.VerificarEscrituraAsync(
            _practicaRepo, _contexto, seguimiento.PracticaId, ct);

        if (seguimiento.Anulado)
            throw new ReglaDeDominioException(
                $"El seguimiento {seguimientoId} esta anulado y no admite observaciones nuevas.",
                "RN-12");

        var observacion = new Observacion(seguimientoId, request.Contenido);

        await _observacionRepo.AgregarAsync(observacion, ct);

        // Igual que en el alta de seguimiento: hasta confirmar, el Id vale 0 y la
        // fecha no existe todavia. La escribe MySQL (RN-11).
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Observacion {ObservacionId} registrada sobre el seguimiento {SeguimientoId} por el instructor {InstructorId}.",
            observacion.Id, seguimientoId, _contexto.UsuarioId);

        return _mapeador.Map<ObservacionResponse>(observacion);
    }
}
