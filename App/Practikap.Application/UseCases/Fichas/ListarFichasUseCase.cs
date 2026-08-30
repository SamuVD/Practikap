using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Fichas;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Fichas;

/// <summary>
/// Lista las fichas de formacion registradas. Reservado al Administrador
/// (FA-26).
/// </summary>
public sealed class ListarFichasUseCase
{
    private readonly IFichaRepository _fichaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="fichaRepo">Acceso a fichas de formacion.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarFichasUseCase(
        IFichaRepository fichaRepo, IContextoUsuario contexto, IMapper mapeador)
    {
        _fichaRepo = fichaRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve todas las fichas de formacion.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de fichas.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    public async Task<IReadOnlyList<FichaResponse>> ExecuteAsync(CancellationToken ct)
    {
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException(
                "Solo el Administrador puede consultar el listado de fichas.");

        var fichas = await _fichaRepo.ListarAsync(ct);

        return _mapeador.Map<IReadOnlyList<FichaResponse>>(fichas);
    }
}
