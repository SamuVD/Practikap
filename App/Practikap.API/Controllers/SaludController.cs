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
/// Los endpoints marcados como solo desarrollo se eliminan en la Fase 4.1.
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

    /// <summary>
    /// SOLO DESARROLLO. Emite un token de prueba para verificar el pipeline sin
    /// exponer la clave de firma en herramientas externas. Se elimina en la
    /// Fase 4.1, cuando exista el caso de uso real de inicio de sesion.
    /// </summary>
    /// <param name="generador">Emisor de tokens.</param>
    /// <param name="rol">Rol que llevara el token. Administrador por defecto.</param>
    /// <param name="usuarioId">Identificador que ira en el claim sub.</param>
    /// <returns>El token emitido y su referencia.</returns>
    [HttpGet("token-de-prueba")]
    [AllowAnonymous]
    public IActionResult TokenDePrueba(
        [FromServices] IGeneradorDeToken generador,
        [FromQuery] string rol = "Administrador",
        [FromQuery] int usuarioId = 1)
    {
        if (!_entorno.IsDevelopment())
            return NotFound();

        var emitido = generador.Generar(usuarioId, "prueba@practikap.local", rol);

        return Ok(new
        {
            token = emitido.Token,
            referenciaToken = emitido.ReferenciaToken,
            expiraEn = emitido.ExpiraEn
        });
    }

    /// <summary>
    /// SOLO DESARROLLO. Lanza una excepcion no controlada para comprobar que el
    /// middleware global responde 500 con el contrato uniforme y sin filtrar la
    /// pila de llamadas. Se elimina en la Fase 4.1.
    /// </summary>
    /// <returns>Nunca retorna.</returns>
    [HttpGet("error")]
    [AllowAnonymous]
    public IActionResult Error()
    {
        if (!_entorno.IsDevelopment())
            return NotFound();

        throw new InvalidOperationException("Fallo simulado para verificar el middleware global de errores.");
    }
}
