using AutoMapper;
using FluentValidation;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Usuarios;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Usuarios;

/// <summary>
/// Actualiza los datos personales de un usuario (RF-01).
/// </summary>
/// <remarks>
/// El Administrador edita cualquier perfil por su alcance Total sobre
/// /api/usuarios; los demas roles solo el propio (decision F2, RN-13).
/// </remarks>
public sealed class ActualizarPerfilUseCase
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<ActualizarPerfilRequest> _validador;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="usuarioRepo">Acceso a usuarios.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ActualizarPerfilUseCase(
        IUsuarioRepository usuarioRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<ActualizarPerfilRequest> validador,
        IMapper mapeador)
    {
        _usuarioRepo = usuarioRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
    }

    /// <summary>Aplica los cambios al perfil indicado.</summary>
    /// <param name="id">Usuario a modificar.</param>
    /// <param name="request">Datos personales nuevos.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El usuario con sus datos ya actualizados.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="AutorizacionException">Si edita a otro sin ser Administrador (403).</exception>
    /// <exception cref="NoEncontradoException">Si el usuario no existe (404).</exception>
    public async Task<UsuarioResponse> ExecuteAsync(
        int id, ActualizarPerfilRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        if (_contexto.Alcance != AlcanceConsulta.Global && _contexto.UsuarioId != id)
            throw new AutorizacionException("Solo puede editar su propio perfil.");

        var usuario = await _usuarioRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Usuario", id);

        usuario.ActualizarDatos(request.Nombre, request.Apellido, request.Telefono);

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        return _mapeador.Map<UsuarioResponse>(usuario);
    }
}