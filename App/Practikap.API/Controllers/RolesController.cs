using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Roles;
using Practikap.Application.UseCases.Roles;

namespace Practikap.API.Controllers;

/// <summary>
/// Catalogo de roles del sistema. Modulo M1 (decision D6).
/// </summary>
/// <remarks>
/// Solo lectura: los tres roles son un catalogo fijo sembrado por la migracion y
/// la aplicacion no crea, edita ni elimina roles en operacion.
/// </remarks>
[ApiController]
[Route("api/roles")]
[Authorize(Roles = "Administrador")]
public sealed class RolesController : ControllerBase
{
    private readonly ListarRolesUseCase _listarRoles;

    /// <summary>Crea el controlador.</summary>
    /// <param name="listarRoles">Caso de uso de listado de roles.</param>
    public RolesController(ListarRolesUseCase listarRoles) => _listarRoles = listarRoles;

    /// <summary>Lista los roles disponibles para asignar a un usuario.</summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Los tres roles del catalogo.</returns>
    /// <response code="200">Catalogo de roles.</response>
    /// <response code="401">Token ausente, expirado o revocado.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RolResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await _listarRoles.ExecuteAsync(ct));
}