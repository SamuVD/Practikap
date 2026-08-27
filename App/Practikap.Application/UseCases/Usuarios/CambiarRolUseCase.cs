using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Usuarios;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Usuarios;

/// <summary>
/// Reasigna el rol de un usuario. Reservado al Administrador (RF-02, RN-01).
/// </summary>
public sealed class CambiarRolUseCase
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IRolRepository _rolRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CambiarRolRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<CambiarRolUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="usuarioRepo">Acceso a usuarios.</param>
    /// <param name="rolRepo">Catalogo de roles.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CambiarRolUseCase(
        IUsuarioRepository usuarioRepo,
        IRolRepository rolRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CambiarRolRequest> validador,
        IMapper mapeador,
        ILogger<CambiarRolUseCase> registro)
    {
        _usuarioRepo = usuarioRepo;
        _rolRepo = rolRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Cambia el rol del usuario indicado.</summary>
    /// <param name="id">Usuario afectado.</param>
    /// <param name="request">Rol destino.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El usuario con su rol ya actualizado.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="NoEncontradoException">Si el usuario no existe (404).</exception>
    /// <exception cref="ReglaDeDominioException">Si cambia su propio rol o el rol no existe (422).</exception>
    public async Task<UsuarioResponse> ExecuteAsync(
        int id, CambiarRolRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        // Autoproteccion (decision F4): un Administrador que se degrade a si
        // mismo pierde el acceso al unico endpoint capaz de revertirlo.
        if (_contexto.UsuarioId == id)
            throw new ReglaDeDominioException(
                "No puede cambiar su propio rol. Solicitelo a otro Administrador.", "RN-01");

        var usuario = await _usuarioRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Usuario", id);

        var roles = await _rolRepo.ListarAsync(ct);
        var rol = roles.FirstOrDefault(candidato => candidato.Id == request.RolId)
            ?? throw new ReglaDeDominioException("El rol indicado no existe.", "RN-01");

        usuario.CambiarRol(rol.Id);

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Rol del usuario {UsuarioId} cambiado a {Rol} por el administrador {AdministradorId}.",
            usuario.Id, rol.Nombre, _contexto.UsuarioId);

        // Se relee para que la navegacion Rol refleje el rol nuevo y no el que
        // tenia cargado la instancia rastreada.
        var actualizado = await _usuarioRepo.ObtenerPorIdAsync(id, ct);
        return _mapeador.Map<UsuarioResponse>(actualizado!);
    }
}