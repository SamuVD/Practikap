using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Programas;
using Practikap.Application.UseCases.Programas;

namespace Practikap.API.Controllers;

/// <summary>
/// Programas de formacion. Modulo M3, dato maestro que gestiona el
/// Administrador (FA-26).
/// </summary>
/// <remarks>
/// La administracion completa del catalogo, con edicion incluida, llega con M8
/// en el paso 4.9. M3 expone solo lo que sus practicas necesitan.
///
/// El GET admite tambien al Instructor, que recibe unicamente los programas
/// derivados de sus practicas asignadas: el vinculo directo instructor-programa
/// no se implementa en v1 (H20, FA-27).
/// </remarks>
[ApiController]
[Route("api/programas")]
[Authorize]
public sealed class ProgramasController : ControllerBase
{
    private readonly ListarProgramasUseCase _listar;
    private readonly CrearProgramaUseCase _crear;

    /// <summary>Crea el controlador.</summary>
    /// <param name="listar">Listado de programas.</param>
    /// <param name="crear">Alta de programa.</param>
    public ProgramasController(ListarProgramasUseCase listar, CrearProgramaUseCase crear)
    {
        _listar = listar;
        _crear = crear;
    }

    /// <summary>Lista los programas de formacion visibles para el solicitante.</summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Coleccion de programas.</returns>
    /// <response code="200">Listado de programas.</response>
    /// <response code="403">El rol autenticado no es Administrador ni Instructor.</response>
    [HttpGet]
    [Authorize(Roles = "Administrador,Instructor")]
    [ProducesResponseType(typeof(IReadOnlyList<ProgramaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await _listar.ExecuteAsync(ct));

    /// <summary>Da de alta un programa de formacion.</summary>
    /// <param name="request">Datos del programa a crear.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El programa creado.</returns>
    /// <response code="201">Programa creado.</response>
    /// <response code="400">Los datos no superan la validacion de forma.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="409">El nombre ya esta registrado.</response>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(ProgramaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearProgramaRequest request, CancellationToken ct)
    {
        var creado = await _crear.ExecuteAsync(request, ct);

        // El recurso no tiene GET por identificador en v1, asi que la cabecera
        // Location apunta a la coleccion, que es donde puede encontrarse.
        return CreatedAtAction(nameof(Listar), creado);
    }
}
