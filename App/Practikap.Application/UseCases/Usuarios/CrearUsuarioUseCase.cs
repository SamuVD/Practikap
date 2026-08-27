using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Usuarios;
using Practikap.Domain.Entities;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Usuarios;

/// <summary>
/// Da de alta un usuario. Reservado al Administrador (RF-02, decision D4: en v1
/// no existe auto-registro).
/// </summary>
public sealed class CrearUsuarioUseCase
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IRolRepository _rolRepo;
    private readonly IServicioDeHash _hasher;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CrearUsuarioRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<CrearUsuarioUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="usuarioRepo">Acceso a usuarios.</param>
    /// <param name="rolRepo">Catalogo de roles.</param>
    /// <param name="hasher">Derivacion de la contrasena (RNF-05).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CrearUsuarioUseCase(
        IUsuarioRepository usuarioRepo,
        IRolRepository rolRepo,
        IServicioDeHash hasher,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CrearUsuarioRequest> validador,
        IMapper mapeador,
        ILogger<CrearUsuarioUseCase> registro)
    {
        _usuarioRepo = usuarioRepo;
        _rolRepo = rolRepo;
        _hasher = hasher;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Crea el usuario y devuelve sus datos.</summary>
    /// <param name="request">Datos del usuario a crear.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El usuario creado, ya con su identificador asignado.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="ConflictoException">Si el correo ya esta registrado (409).</exception>
    /// <exception cref="ReglaDeDominioException">Si el rol indicado no existe (422).</exception>
    public async Task<UsuarioResponse> ExecuteAsync(CrearUsuarioRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        if (await _usuarioRepo.ExisteCorreoAsync(request.Correo, ct))
            throw new ConflictoException($"Ya existe un usuario con el correo {request.Correo}.");

        // El catalogo tiene tres filas: se comprueba en memoria en lugar de
        // ampliar IRolRepository con una consulta por identificador (F6).
        var roles = await _rolRepo.ListarAsync(ct);
        var rol = roles.FirstOrDefault(candidato => candidato.Id == request.RolId)
            ?? throw new ReglaDeDominioException("El rol indicado no existe.", "RN-01");

        var usuario = new Usuario(
            rol.Id,
            request.Correo,
            _hasher.Hash(request.Contrasena),
            request.Nombre,
            request.Apellido,
            request.Telefono);

        await _usuarioRepo.AgregarAsync(usuario, ct);

        // Hasta aqui usuario.Id vale 0. Es la confirmacion la que le asigna el
        // valor real, asi que el mapeo va despues y no antes.
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Usuario {UsuarioId} creado con rol {Rol}.", usuario.Id, rol.Nombre);

        return _mapeador.Map<UsuarioResponse>(usuario);
    }
}