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
/// Registra la calificacion que el Instructor emite sobre el Aprendiz de una
/// practica (RF-06, CU-05, J1).
/// </summary>
/// <remarks>
/// No consulta ni depende en ningun punto de la direccion contraria. Eso es
/// RN-10: el instructor puede calificar sin que el aprendiz lo haya evaluado, y
/// al reves. Las dos direcciones son registros independientes en tablas
/// separadas.
///
/// Admite varias calificaciones por practica (J5). No hay comprobacion de
/// duplicado ni 409: CU-05 describe una evaluacion que se repite a lo largo de la
/// practica, y es esa serie la que el promedio vigente resume.
///
/// El caso de uso no escribe la fecha en ningun momento, ni la recibe: la genera
/// MySQL al insertar. Eso es RN-11, y se sostiene en tres puntos que no dependen
/// entre si: el DTO de entrada no tiene donde declararla, la entidad expone
/// FechaRegistro con setter privado, y la columna esta mapeada como generada por
/// la base.
/// </remarks>
public sealed class RegistrarCalificacionInstructorUseCase
{
    private readonly ICalificacionInstructorRepository _calificacionRepo;
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CrearCalificacionRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<RegistrarCalificacionInstructorUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="calificacionRepo">Acceso a las calificaciones del instructor.</param>
    /// <param name="practicaRepo">Acceso a practicas, para las puertas de J4 y de autoria.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public RegistrarCalificacionInstructorUseCase(
        ICalificacionInstructorRepository calificacionRepo,
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CrearCalificacionRequest> validador,
        IMapper mapeador,
        ILogger<RegistrarCalificacionInstructorUseCase> registro)
    {
        _calificacionRepo = calificacionRepo;
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Registra la calificacion y devuelve sus datos.</summary>
    /// <param name="request">Practica, valor y comentario.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La calificacion creada, con la fecha que fijo el servidor.</returns>
    /// <exception cref="ValidationException">
    /// Si el DTO no supera la validacion de forma, incluido el valor fuera del
    /// rango 0.0 a 5.0 (400).
    /// </exception>
    /// <exception cref="AutorizacionException">
    /// Si el solicitante no es el instructor responsable de la practica (403).
    /// </exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si la practica no existe, o si no esta En curso ni En riesgo (422, J4).
    /// </exception>
    public async Task<CalificacionResponse> ExecuteAsync(
        CrearCalificacionRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        await AccesoALaPractica.VerificarEscrituraAsync(
            _practicaRepo, _contexto, request.PracticaId, ct);

        var calificacion = new CalificacionInstructor(
            request.PracticaId, request.Valor, request.Comentario);

        await _calificacionRepo.AgregarAsync(calificacion, ct);

        // Hasta aqui calificacion.Id vale 0 y FechaRegistro es el valor por
        // defecto de DateTime. La confirmacion asigna el primero y trae de vuelta
        // la segunda, que es la que escribio MySQL.
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        // Punto de enganche del Motor de Reglas (RN-06). CU-05 pide que el Motor
        // evalue el estado de la practica despues de cada calificacion y, si el
        // promedio cae bajo el umbral, la marque En riesgo y notifique (RN-09).
        // El Motor llega en el paso 4.7: aqui no se implementa ni se simula.
        // Cuando llegue, consumira PromedioVigenteAsync de este mismo repositorio.

        _registro.LogInformation(
            "Calificacion {CalificacionId} del instructor {InstructorId} registrada sobre la practica {PracticaId}.",
            calificacion.Id, _contexto.UsuarioId, calificacion.PracticaId);

        // Sin relectura, a diferencia de RegistrarSeguimientoUseCase: aquel relee
        // para materializar Observaciones y aqui no hay coleccion anidada que
        // poblar.
        return _mapeador.Map<CalificacionResponse>(calificacion);
    }
}
