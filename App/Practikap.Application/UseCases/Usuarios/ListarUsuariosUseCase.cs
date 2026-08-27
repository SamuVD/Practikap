using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Usuarios;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Usuarios;

/// <summary>
/// Lista los usuarios del sistema, con filtro opcional por rol (RF-02).
/// </summary>
/// <remarks>
/// Reservado al alcance Global de RN-13. La comprobacion vive aqui y no solo en
/// el atributo del controlador: ADR-03 situa el aislamiento por rol en la capa
/// de Aplicacion, para que sea verificable sin servidor web (RNF-08).
/// </remarks>
public sealed class ListarUsuariosUseCase
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="usuarioRepo">Acceso a usuarios.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarUsuariosUseCase(
        IUsuarioRepository usuarioRepo, IContextoUsuario contexto, IMapper mapeador)
    {
        _usuarioRepo = usuarioRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve los usuarios visibles para el solicitante.</summary>
    /// <param name="rolId">Rol por el que filtrar. Null devuelve todos.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de usuarios.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    public async Task<IReadOnlyList<UsuarioResponse>> ExecuteAsync(int? rolId, CancellationToken ct)
    {
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException("Solo el Administrador puede consultar el listado de usuarios.");

        var usuarios = rolId is null
            ? await _usuarioRepo.ListarTodosAsync(ct)
            : await _usuarioRepo.ListarPorRolAsync(rolId.Value, ct);

        return _mapeador.Map<IReadOnlyList<UsuarioResponse>>(usuarios);
    }
}