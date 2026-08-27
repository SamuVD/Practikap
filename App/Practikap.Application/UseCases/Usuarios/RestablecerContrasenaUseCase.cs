using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Usuarios;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Usuarios;

/// <summary>
/// Restablece la contrasena de un usuario sin exigir la actual. Reservado al
/// Administrador (decision D8).
/// </summary>
/// <remarks>
/// Existe porque en v1 no hay recuperacion autoservicio por correo (FA-21): si
/// alguien olvida su contrasena, la unica salida es que el Administrador se la
/// restablezca.
///
/// No revoca los tokens del usuario afectado, porque el Administrador no conoce
/// su claim jti y tokens_revocados solo indexa por esa referencia. La sesion que
/// ese usuario tuviera abierta sigue viva hasta expirar; esta ventana esta
/// catalogada como FA-20.
/// </remarks>
public sealed class RestablecerContrasenaUseCase
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IServicioDeHash _hasher;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<RestablecerContrasenaRequest> _validador;
    private readonly ILogger<RestablecerContrasenaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="usuarioRepo">Acceso a usuarios.</param>
    /// <param name="hasher">Derivacion de la contrasena (RNF-05).</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="registro">Registro de eventos.</param>
    public RestablecerContrasenaUseCase(
        IUsuarioRepository usuarioRepo,
        IServicioDeHash hasher,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<RestablecerContrasenaRequest> validador,
        ILogger<RestablecerContrasenaUseCase> registro)
    {
        _usuarioRepo = usuarioRepo;
        _hasher = hasher;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _registro = registro;
    }

    /// <summary>Asigna una contrasena nueva al usuario indicado.</summary>
    /// <param name="id">Usuario afectado.</param>
    /// <param name="request">Contrasena de reemplazo.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="NoEncontradoException">Si el usuario no existe (404).</exception>
    public async Task ExecuteAsync(int id, RestablecerContrasenaRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        var usuario = await _usuarioRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Usuario", id);

        usuario.CambiarContrasena(_hasher.Hash(request.ContrasenaNueva));

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Contrasena del usuario {UsuarioId} restablecida por el administrador {AdministradorId}.",
            usuario.Id, _contexto.UsuarioId);
    }
}