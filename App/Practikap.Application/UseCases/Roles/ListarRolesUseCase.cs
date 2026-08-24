using AutoMapper;
using Practikap.Application.DTOs.Roles;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Roles;

/// <summary>
/// Lista el catalogo de roles (decision D6). Alimenta el selector de rol de
/// gestion-usuarios.html.
/// </summary>
/// <remarks>
/// No comprueba alcance: la restriccion al Administrador la aplica el atributo
/// del controlador. El catalogo no contiene datos de ninguna persona, asi que
/// RN-13 no tiene nada que aislar aqui.
/// </remarks>
public sealed class ListarRolesUseCase
{
    private readonly IRolRepository _rolRepo;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="rolRepo">Catalogo de roles.</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarRolesUseCase(IRolRepository rolRepo, IMapper mapeador)
    {
        _rolRepo = rolRepo;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve los roles disponibles.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de roles.</returns>
    public async Task<IReadOnlyList<RolResponse>> ExecuteAsync(CancellationToken ct) =>
        _mapeador.Map<IReadOnlyList<RolResponse>>(await _rolRepo.ListarAsync(ct));
}