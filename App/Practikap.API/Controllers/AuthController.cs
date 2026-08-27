using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Autenticacion;
using Practikap.Application.UseCases.Autenticacion;

namespace Practikap.API.Controllers;

/// <summary>
/// Autenticacion de la plataforma. Modulo M1, CU-01 (RF-01).
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IniciarSesionUseCase _iniciarSesion;
    private readonly CerrarSesionUseCase _cerrarSesion;

    /// <summary>Crea el controlador.</summary>
    /// <param name="iniciarSesion">Caso de uso de inicio de sesion.</param>
    /// <param name="cerrarSesion">Caso de uso de cierre de sesion.</param>
    public AuthController(IniciarSesionUseCase iniciarSesion, CerrarSesionUseCase cerrarSesion)
    {
        _iniciarSesion = iniciarSesion;
        _cerrarSesion = cerrarSesion;
    }

    /// <summary>
    /// Verifica las credenciales y emite un JWT (RN-02). Unico endpoint publico
    /// del sistema junto con el de estado.
    /// </summary>
    /// <param name="request">Correo y contrasena.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El token emitido y los datos del usuario autenticado.</returns>
    /// <response code="200">Sesion iniciada.</response>
    /// <response code="400">El correo o la contrasena no superan la validacion de forma.</response>
    /// <response code="401">Credenciales incorrectas o cuenta inactiva.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await _iniciarSesion.ExecuteAsync(request, ct));

    /// <summary>
    /// Cierra la sesion en curso registrando su token como revocado (RN-03).
    /// </summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">Sesion cerrada y token revocado.</response>
    /// <response code="401">Token ausente, expirado o ya revocado.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _cerrarSesion.ExecuteAsync(ct);
        return NoContent();
    }
}