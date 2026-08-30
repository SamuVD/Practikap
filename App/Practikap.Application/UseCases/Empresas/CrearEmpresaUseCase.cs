using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Empresas;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Empresas;

/// <summary>
/// Da de alta una empresa receptora. Reservado al Administrador (FA-26).
/// </summary>
public sealed class CrearEmpresaUseCase
{
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CrearEmpresaRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<CrearEmpresaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="empresaRepo">Acceso a empresas receptoras.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CrearEmpresaUseCase(
        IEmpresaRepository empresaRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CrearEmpresaRequest> validador,
        IMapper mapeador,
        ILogger<CrearEmpresaUseCase> registro)
    {
        _empresaRepo = empresaRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Crea la empresa y devuelve sus datos.</summary>
    /// <param name="request">Datos de la empresa a crear.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La empresa creada, ya con su identificador asignado.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    /// <exception cref="ConflictoException">Si el NIT ya esta registrado (409).</exception>
    public async Task<EmpresaResponse> ExecuteAsync(CrearEmpresaRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException("Solo el Administrador puede crear empresas.");

        if (await _empresaRepo.ExisteNitAsync(request.Nit, ct))
            throw new ConflictoException($"Ya existe una empresa con el NIT {request.Nit}.");

        var empresa = new Empresa(
            request.RazonSocial,
            request.Nit,
            request.JefeInmediatoNombre,
            request.JefeInmediatoCorreo,
            request.JefeInmediatoTelefono);

        await _empresaRepo.AgregarAsync(empresa, ct);

        // Hasta aqui empresa.Id vale 0 (ADR-02).
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation("Empresa {EmpresaId} creada.", empresa.Id);

        return _mapeador.Map<EmpresaResponse>(empresa);
    }
}
