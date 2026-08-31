using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Calificaciones;
using Practikap.Application.UseCases.Seguimientos;
using Practikap.Domain.Entities;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Calificaciones;

/// <summary>
/// Registra la evaluacion que el Aprendiz emite sobre el Instructor de su
/// practica (RF-06, CU-05, HU-07, J1).
/// </summary>
/// <remarks>
/// Gemelo de <see cref="RegistrarCalificacionInstructorUseCase"/> sobre la otra
/// tabla, y deliberadamente sin nada compartido con el: RN-10 exige que las dos
/// direcciones sean independientes, y factorizarlas en una base comun habria
/// creado justo el acoplamiento que la regla prohibe. La unica pieza que si
/// comparten es la puerta de acceso a la practica, que no es de M5 sino de M4.
///
/// La unica diferencia de comportamiento esta en quien puede escribir: aqui el
/// solicitante debe ser el aprendiz de la practica, no su instructor.
///
/// Como en la direccion contraria, la fecha la fija el servidor (RN-11) y se
/// admiten varias evaluaciones por practica (J5).
///
/// Notifica al instructor evaluado (RF-07, L5). Es la unica diferencia del
/// enganche respecto de la direccion contraria: mismo metodo del generador y
/// mismo texto, otro destinatario.
///
/// <b>No dispara el Motor de Reglas, y esa es la asimetria deliberada de N12.</b>
/// La direccion contraria si lo hace. RN-09 mide el riesgo del aprendiz, y la nota
/// que el aprendiz le pone a su instructor no lo mide: marcar la practica En riesgo
/// porque el aprendiz califico bajo invertiria el sentido de la regla y castigaria
/// al aprendiz por ejercer la evaluacion que RF-06 le concede.
/// </remarks>
public sealed class RegistrarCalificacionAprendizUseCase
{
    private readonly ICalificacionAprendizRepository _calificacionRepo;
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IGeneradorDeNotificaciones _generador;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CrearCalificacionRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<RegistrarCalificacionAprendizUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="calificacionRepo">Acceso a las evaluaciones del aprendiz.</param>
    /// <param name="practicaRepo">Acceso a practicas, para las puertas de J4 y de autoria.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="generador">Emision de la notificacion de RF-07 (L5, L6).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public RegistrarCalificacionAprendizUseCase(
        ICalificacionAprendizRepository calificacionRepo,
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        IGeneradorDeNotificaciones generador,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CrearCalificacionRequest> validador,
        IMapper mapeador,
        ILogger<RegistrarCalificacionAprendizUseCase> registro)
    {
        _calificacionRepo = calificacionRepo;
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _generador = generador;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Registra la evaluacion y devuelve sus datos.</summary>
    /// <param name="request">Practica, valor y comentario.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La evaluacion creada, con la fecha que fijo el servidor.</returns>
    /// <exception cref="ValidationException">
    /// Si el DTO no supera la validacion de forma, incluido el valor fuera del
    /// rango 0.0 a 5.0 (400).
    /// </exception>
    /// <exception cref="AutorizacionException">
    /// Si el solicitante no es el aprendiz de la practica (403).
    /// </exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si la practica no existe, o si no esta En curso ni En riesgo (422, J4).
    /// </exception>
    public async Task<CalificacionResponse> ExecuteAsync(
        CrearCalificacionRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        // La practica se captura y ya no se descarta: de ella sale el instructor
        // al que se notifica. No hay consulta nueva, es la misma que la puerta ya
        // hacia.
        var practica = await AccesoALaPractica.VerificarEscrituraDelAprendizAsync(
            _practicaRepo, _contexto, request.PracticaId, ct);

        var calificacion = new CalificacionAprendiz(
            request.PracticaId, request.Valor, request.Comentario);

        await _calificacionRepo.AgregarAsync(calificacion, ct);

        // L5, igual que en la direccion contraria y con el mismo metodo del
        // generador: cambia el destinatario, que aqui es el instructor. Va antes
        // de la confirmacion, de modo que las dos filas caen en la misma
        // transaccion.
        await _generador.PorCalificacionAsync(practica.InstructorId, practica.Id, ct);

        // No hay llamada al Motor, a diferencia de la direccion contraria. La
        // Ronda 2 del 4.7 retiro de aqui el enganche que el 4.4 habia marcado, por
        // la razon que N12 fija y que el remarks de la clase explica: esta
        // calificacion no mide al aprendiz.
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Evaluacion {CalificacionId} del aprendiz {AprendizId} registrada sobre la practica {PracticaId}.",
            calificacion.Id, _contexto.UsuarioId, calificacion.PracticaId);

        return _mapeador.Map<CalificacionResponse>(calificacion);
    }
}
