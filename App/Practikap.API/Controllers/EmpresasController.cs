using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Empresas;
using Practikap.Application.UseCases.Empresas;

namespace Practikap.API.Controllers;

/// <summary>
/// Empresas receptoras. Modulo M3, dato maestro que gestiona el Administrador
/// (FA-26).
/// </summary>
/// <remarks>
/// El recurso entero esta reservado al Administrador, de modo que la restriccion
/// por rol va a nivel de clase. La segunda barrera sigue viviendo en los casos de
/// uso (ADR-03).
/// </remarks>
[ApiController]
[Route("api/empresas")]
[Authorize(Roles = "Administrador")]
public sealed class EmpresasController : ControllerBase
{
    private readonly ListarEmpresasUseCase _listar;
    private readonly CrearEmpresaUseCase _crear;

    /// <summary>Crea el controlador.</summary>
    /// <param name="listar">Listado de empresas.</param>
    /// <param name="crear">Alta de empresa.</param>
    public EmpresasController(ListarEmpresasUseCase listar, CrearEmpresaUseCase crear)
    {
        _listar = listar;
        _crear = crear;
    }

    /// <summary>Lista las empresas receptoras registradas.</summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Coleccion de empresas.</returns>
    /// <response code="200">Listado de empresas.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmpresaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await _listar.ExecuteAsync(ct));

    /// <summary>Da de alta una empresa receptora.</summary>
    /// <param name="request">Datos de la empresa a crear.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La empresa creada.</returns>
    /// <response code="201">Empresa creada.</response>
    /// <response code="400">Los datos no superan la validacion de forma.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="409">El NIT ya esta registrado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(EmpresaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearEmpresaRequest request, CancellationToken ct)
    {
        var creada = await _crear.ExecuteAsync(request, ct);

        // El recurso no tiene GET por identificador en v1, asi que la cabecera
        // Location apunta a la coleccion, que es donde puede encontrarse.
        return CreatedAtAction(nameof(Listar), creada);
    }
}
