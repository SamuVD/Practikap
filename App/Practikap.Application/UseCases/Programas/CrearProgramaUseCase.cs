using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Programas;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Programas;

/// <summary>
/// Da de alta un programa de formacion. Reservado al Administrador (FA-26).
/// </summary>
/// <remarks>
/// M3 gestiona los datos maestros que sus practicas necesitan. La administracion
/// completa del catalogo, con edicion incluida, llega con M8 en el paso 4.9:
/// ProgramaRepository.ActualizarAsync ya esta previsto para entonces.
/// </remarks>
public sealed class CrearProgramaUseCase
{
    private readonly IProgramaRepository _programaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CrearProgramaRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<CrearProgramaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="programaRepo">Acceso a programas de formacion.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CrearProgramaUseCase(
        IProgramaRepository programaRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CrearProgramaRequest> validador,
        IMapper mapeador,
        ILogger<CrearProgramaUseCase> registro)
    {
        _programaRepo = programaRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Crea el programa y devuelve sus datos.</summary>
    /// <param name="request">Datos del programa a crear.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El programa creado, ya con su identificador asignado.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    /// <exception cref="ConflictoException">Si el nombre ya esta registrado (409).</exception>
    public async Task<ProgramaResponse> ExecuteAsync(CrearProgramaRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException("Solo el Administrador puede crear programas de formacion.");

        if (await _programaRepo.ExisteNombreAsync(request.Nombre, ct))
            throw new ConflictoException($"Ya existe un programa con el nombre {request.Nombre}.");

        var programa = new Programa(request.Nombre, request.Descripcion);

        await _programaRepo.AgregarAsync(programa, ct);

        // Hasta aqui programa.Id vale 0 (ADR-02).
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation("Programa {ProgramaId} creado.", programa.Id);

        return _mapeador.Map<ProgramaResponse>(programa);
    }
}
