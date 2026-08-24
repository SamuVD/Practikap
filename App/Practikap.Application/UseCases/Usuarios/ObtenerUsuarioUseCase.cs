using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Usuarios;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Usuarios;

/// <summary>
/// Devuelve un usuario por su identificador (RF-02).
/// </summary>
/// <remarks>
/// El Administrador consulta a cualquiera; los demas roles solo a si mismos
/// (RN-13). Alimenta mi-perfil.html para los tres roles.
/// </remarks>
public sealed class ObtenerUsuarioUseCase
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="usuarioRepo">Acceso a usuarios.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ObtenerUsuarioUseCase(
        IUsuarioRepository usuarioRepo, IContextoUsuario contexto, IMapper mapeador)
    {
        _usuarioRepo = usuarioRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Obtiene el usuario solicitado.</summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Datos del usuario.</returns>
    /// <exception cref="AutorizacionException">Si consulta a otro sin ser Administrador (403).</exception>
    /// <exception cref="NoEncontradoException">Si el usuario no existe (404).</exception>
    public async Task<UsuarioResponse> ExecuteAsync(int id, CancellationToken ct)
    {
        // El 403 se decide antes de consultar: responder 404 a quien no tiene
        // permiso revelaria que identificadores existen y cuales no.
        if (_contexto.Alcance != AlcanceConsulta.Global && _contexto.UsuarioId != id)
            throw new AutorizacionException("Solo puede consultar su propio perfil.");

        var usuario = await _usuarioRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Usuario", id);

        return _mapeador.Map<UsuarioResponse>(usuario);
    }
}