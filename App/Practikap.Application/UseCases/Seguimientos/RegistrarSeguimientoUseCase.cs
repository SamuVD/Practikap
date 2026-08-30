using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Seguimientos;
using Practikap.Domain.Entities;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Seguimientos;

/// <summary>
/// Registra un seguimiento sobre una practica. Reservado al Instructor
/// responsable (RF-04, CU-04, I7).
/// </summary>
/// <remarks>
/// El caso de uso no escribe la fecha en ningun momento, ni la recibe: la genera
/// MySQL al insertar. Eso es RN-11, y se sostiene en tres puntos que no dependen
/// entre si: el DTO de entrada no tiene donde declararla, la entidad expone
/// FechaRegistro con setter privado, y la columna esta mapeada como generada por
/// la base.
/// </remarks>
public sealed class RegistrarSeguimientoUseCase
{
    private readonly ISeguimientoRepository _seguimientoRepo;
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CrearSeguimientoRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<RegistrarSeguimientoUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="seguimientoRepo">Acceso a seguimientos.</param>
    /// <param name="practicaRepo">Acceso a practicas, para las puertas de I2 e I7.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public RegistrarSeguimientoUseCase(
        ISeguimientoRepository seguimientoRepo,
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CrearSeguimientoRequest> validador,
        IMapper mapeador,
        ILogger<RegistrarSeguimientoUseCase> registro)
    {
        _seguimientoRepo = seguimientoRepo;
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Registra el seguimiento y devuelve sus datos.</summary>
    /// <param name="request">Practica, avance y etapa.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El seguimiento creado, con la fecha que fijo el servidor.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="AutorizacionException">
    /// Si el solicitante no es el instructor responsable de la practica (403, I7).
    /// </exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si la practica no existe, o si no esta En curso ni En riesgo (422, I2).
    /// </exception>
    public async Task<SeguimientoResponse> ExecuteAsync(
        CrearSeguimientoRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        await AccesoALaPractica.VerificarEscrituraAsync(
            _practicaRepo, _contexto, request.PracticaId, ct);

        var seguimiento = new Seguimiento(request.PracticaId, request.Avance, request.Etapa);

        await _seguimientoRepo.AgregarAsync(seguimiento, ct);

        // Hasta aqui seguimiento.Id vale 0 y FechaRegistro es el valor por
        // defecto de DateTime. La confirmacion asigna el primero y trae de vuelta
        // la segunda, que es la que escribio MySQL.
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Seguimiento {SeguimientoId} registrado sobre la practica {PracticaId} por el instructor {InstructorId}.",
            seguimiento.Id, seguimiento.PracticaId, _contexto.UsuarioId);

        // Se relee para que Observaciones quede materializada como coleccion
        // vacia y no como una navegacion sin cargar. Mismo criterio que
        // CrearPracticaUseCase, que relee para poblar su grafo.
        var creado = await _seguimientoRepo.ObtenerPorIdAsync(seguimiento.Id, ct);
        return _mapeador.Map<SeguimientoResponse>(creado!);
    }
}
