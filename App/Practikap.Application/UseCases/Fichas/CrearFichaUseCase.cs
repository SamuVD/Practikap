using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Fichas;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Fichas;

/// <summary>
/// Da de alta una ficha de formacion. Reservado al Administrador (FA-26).
/// </summary>
public sealed class CrearFichaUseCase
{
    private readonly IFichaRepository _fichaRepo;
    private readonly IProgramaRepository _programaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CrearFichaRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<CrearFichaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="fichaRepo">Acceso a fichas de formacion.</param>
    /// <param name="programaRepo">Acceso a programas, para verificar la referencia.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CrearFichaUseCase(
        IFichaRepository fichaRepo,
        IProgramaRepository programaRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CrearFichaRequest> validador,
        IMapper mapeador,
        ILogger<CrearFichaUseCase> registro)
    {
        _fichaRepo = fichaRepo;
        _programaRepo = programaRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Crea la ficha y devuelve sus datos.</summary>
    /// <param name="request">Datos de la ficha a crear.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La ficha creada, ya con su identificador asignado.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    /// <exception cref="ConflictoException">Si el numero de ficha ya esta registrado (409).</exception>
    /// <exception cref="ReglaDeDominioException">Si el programa indicado no existe (422).</exception>
    public async Task<FichaResponse> ExecuteAsync(CrearFichaRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException("Solo el Administrador puede crear fichas de formacion.");

        if (await _programaRepo.ObtenerPorIdAsync(request.ProgramaId, ct) is null)
            throw new ReglaDeDominioException("El programa de formacion indicado no existe.");

        if (await _fichaRepo.ObtenerPorNumeroAsync(request.NumeroFicha, ct) is not null)
            throw new ConflictoException($"Ya existe una ficha con el numero {request.NumeroFicha}.");

        var ficha = new Ficha(request.NumeroFicha, request.ProgramaId);

        await _fichaRepo.AgregarAsync(ficha, ct);

        // Hasta aqui ficha.Id vale 0 (ADR-02).
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Ficha {FichaId} creada en el programa {ProgramaId}.", ficha.Id, ficha.ProgramaId);

        // Se relee para que la navegacion Programa quede cargada: la ficha se
        // construyo con el identificador del programa, no con la entidad, y el
        // perfil de AutoMapper aplana su nombre.
        var creada = await _fichaRepo.ObtenerPorIdAsync(ficha.Id, ct);
        return _mapeador.Map<FichaResponse>(creada!);
    }
}
