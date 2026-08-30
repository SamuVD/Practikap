using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Fichas;
using Practikap.Application.UseCases.Fichas;

namespace Practikap.API.Controllers;

/// <summary>
/// Fichas de formacion. Modulo M3, dato maestro que gestiona el Administrador
/// (FA-26).
/// </summary>
/// <remarks>
/// El recurso entero esta reservado al Administrador, de modo que la restriccion
/// por rol va a nivel de clase. La segunda barrera sigue viviendo en los casos de
/// uso (ADR-03).
/// </remarks>
[ApiController]
[Route("api/fichas")]
[Authorize(Roles = "Administrador")]
public sealed class FichasController : ControllerBase
{
    private readonly ListarFichasUseCase _listar;
    private readonly CrearFichaUseCase _crear;

    /// <summary>Crea el controlador.</summary>
    /// <param name="listar">Listado de fichas.</param>
    /// <param name="crear">Alta de ficha.</param>
    public FichasController(ListarFichasUseCase listar, CrearFichaUseCase crear)
    {
        _listar = listar;
        _crear = crear;
    }

    /// <summary>Lista las fichas de formacion registradas.</summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Coleccion de fichas.</returns>
    /// <response code="200">Listado de fichas.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FichaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await _listar.ExecuteAsync(ct));

    /// <summary>Da de alta una ficha de formacion.</summary>
    /// <param name="request">Datos de la ficha a crear.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La ficha creada.</returns>
    /// <response code="201">Ficha creada.</response>
    /// <response code="400">Los datos no superan la validacion de forma.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="409">El numero de ficha ya esta registrado.</response>
    /// <response code="422">El programa de formacion indicado no existe.</response>
    [HttpPost]
    [ProducesResponseType(typeof(FichaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Crear(CrearFichaRequest request, CancellationToken ct)
    {
        var creada = await _crear.ExecuteAsync(request, ct);

        // El recurso no tiene GET por identificador en v1, asi que la cabecera
        // Location apunta a la coleccion, que es donde puede encontrarse.
        return CreatedAtAction(nameof(Listar), creada);
    }
}
