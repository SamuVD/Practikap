using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Usuarios;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Usuarios;

/// <summary>
/// Cambia la contrasena del propio usuario y revoca su token en curso
/// (decision D7, RN-03).
/// </summary>
/// <remarks>
/// A diferencia del restablecimiento administrativo, aqui se exige la contrasena
/// actual: sin ella, cualquiera que encontrara una sesion abierta podria
/// apropiarse de la cuenta de forma permanente.
///
/// Nadie puede cambiar la contrasena de otro por esta via, ni siquiera el
/// Administrador, que dispone del endpoint de restablecimiento.
/// </remarks>
public sealed class CambiarContrasenaUseCase
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ITokenRevocadoRepository _tokenRepo;
    private readonly IServicioDeHash _hasher;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CambiarContrasenaRequest> _validador;
    private readonly ILogger<CambiarContrasenaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="usuarioRepo">Acceso a usuarios.</param>
    /// <param name="tokenRepo">Lista de revocacion (RN-03).</param>
    /// <param name="hasher">Verificacion y derivacion de contrasenas (RNF-05).</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="registro">Registro de eventos.</param>
    public CambiarContrasenaUseCase(
        IUsuarioRepository usuarioRepo,
        ITokenRevocadoRepository tokenRepo,
        IServicioDeHash hasher,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CambiarContrasenaRequest> validador,
        ILogger<CambiarContrasenaUseCase> registro)
    {
        _usuarioRepo = usuarioRepo;
        _tokenRepo = tokenRepo;
        _hasher = hasher;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _registro = registro;
    }

    /// <summary>Reemplaza la contrasena y cierra la sesion en curso.</summary>
    /// <param name="id">Usuario afectado. Debe coincidir con el solicitante.</param>
    /// <param name="request">Contrasena actual y nueva.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="AutorizacionException">Si intenta cambiar la de otro usuario (403).</exception>
    /// <exception cref="CredencialesInvalidasException">Si la contrasena actual no coincide (401).</exception>
    /// <exception cref="NoEncontradoException">Si el usuario no existe (404).</exception>
    public async Task ExecuteAsync(int id, CambiarContrasenaRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        if (_contexto.UsuarioId != id)
            throw new AutorizacionException(
                "Solo puede cambiar su propia contrasena. Un Administrador dispone del restablecimiento.");

        var usuario = await _usuarioRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Usuario", id);

        if (!_hasher.Verificar(request.ContrasenaActual, usuario.ContrasenaHash))
            throw new CredencialesInvalidasException("La contrasena actual no es correcta.");

        usuario.CambiarContrasena(_hasher.Hash(request.ContrasenaNueva));

        // El token vigente se emitio bajo la credencial anterior: se revoca para
        // que el cambio surta efecto de inmediato (RN-03). El cliente debe
        // volver a iniciar sesion.
        await _tokenRepo.RegistrarAsync(
            new TokenRevocado(usuario.Id, _contexto.ReferenciaToken, MotivoRevocacion.CambioContrasena),
            ct);

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation("Contrasena cambiada por el propio usuario {UsuarioId}.", usuario.Id);
    }
}