using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Empresas;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Empresas;

/// <summary>
/// Lista las empresas receptoras registradas. Reservado al Administrador
/// (FA-26).
/// </summary>
public sealed class ListarEmpresasUseCase
{
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="empresaRepo">Acceso a empresas receptoras.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarEmpresasUseCase(
        IEmpresaRepository empresaRepo, IContextoUsuario contexto, IMapper mapeador)
    {
        _empresaRepo = empresaRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve todas las empresas receptoras.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de empresas.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    public async Task<IReadOnlyList<EmpresaResponse>> ExecuteAsync(CancellationToken ct)
    {
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException(
                "Solo el Administrador puede consultar el listado de empresas.");

        var empresas = await _empresaRepo.ListarAsync(ct);

        return _mapeador.Map<IReadOnlyList<EmpresaResponse>>(empresas);
    }
}
