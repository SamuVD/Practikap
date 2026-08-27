using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;

namespace Practikap.API.Controllers;

/// <summary>
/// Endpoints de diagnostico del pipeline. No pertenece a ningun modulo funcional
/// ni aparece en la Matriz_de_Roles: existe para verificar que la autenticacion,
/// la autorizacion por rol, CORS y el contrato de error responden como se espera.
/// </summary>
/// <remarks>
/// Los dos endpoints de solo desarrollo que tuvo esta clase (token-de-prueba y
/// error) se eliminaron en la Ronda 3 de la Fase 4.1, al existir el caso de uso
/// real de inicio de sesion (FA-04). Los tres restantes son permanentes.
/// </remarks>
[ApiController]
[Route("api/salud")]
public sealed class SaludController : ControllerBase
{
    private readonly IWebHostEnvironment _entorno;

    /// <summary>Crea el controlador.</summary>
    /// <param name="entorno">Entorno de ejecucion, usado para cerrar los endpoints de diagnostico fuera de desarrollo.</param>
    public SaludController(IWebHostEnvironment entorno) => _entorno = entorno;

    /// <summary>Comprueba que la API responde. No requiere autenticacion.</summary>
    /// <returns>Estado, entorno y hora del servidor.</returns>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Estado() => Ok(new
    {
        estado = "activo",
        entorno = _entorno.EnvironmentName,
        fechaUtc = DateTime.UtcNow
    });

    /// <summary>
    /// Comprueba que el token se valida correctamente. Responde 401 sin token
    /// valido (RN-02) y 401 tambien si el token fue revocado (RN-03).
    /// </summary>
    /// <param name="contexto">Contexto del usuario autenticado (ADR-03).</param>
    /// <returns>Identidad resuelta a partir de los claims.</returns>
    [HttpGet("protegido")]
    [Authorize]
    public IActionResult Protegido([FromServices] IContextoUsuario contexto) => Ok(new
    {
        usuarioId = contexto.UsuarioId,
        rol = contexto.Rol,
        alcance = contexto.Alcance.ToString(),
        referenciaToken = contexto.ReferenciaToken
    });

    /// <summary>
    /// Comprueba la autorizacion por rol sobre el claim role (RN-01). Responde
    /// 403 a Instructor y Aprendiz.
    /// </summary>
    /// <returns>Confirmacion de acceso.</returns>
    [HttpGet("admin")]
    [Authorize(Roles = "Administrador")]
    public IActionResult SoloAdministrador() => Ok(new { mensaje = "Acceso concedido al rol Administrador." });
}
