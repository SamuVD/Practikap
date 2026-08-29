using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Practicas;
using Practikap.Application.UseCases.Practicas;

namespace Practikap.API.Controllers;

/// <summary>
/// Gestion de practicas productivas. Modulo M3, CU-03 (RF-03, RN-04, RN-05,
/// RN-13).
/// </summary>
/// <remarks>
/// No expone DELETE: una practica cambia de estado dentro del ciclo de vida de
/// RN-05, nunca se elimina (decision F3).
///
/// Las tres operaciones de escritura llevan [Authorize(Roles = "Administrador")]
/// porque H17 se las reserva. Las dos de lectura no llevan restriccion por rol:
/// los tres alcances de RN-13 los resuelve el caso de uso sobre IContextoUsuario,
/// que es donde ADR-03 los pone.
/// </remarks>
[ApiController]
[Route("api/practicas")]
[Authorize]
public sealed class PracticasController : ControllerBase
{
    private readonly ListarPracticasUseCase _listar;
    private readonly ObtenerPracticaUseCase _obtener;
    private readonly CrearPracticaUseCase _crear;
    private readonly ActualizarPracticaUseCase _actualizar;
    private readonly CambiarEstadoPracticaUseCase _cambiarEstado;

    /// <summary>Crea el controlador.</summary>
    /// <param name="listar">Listado de practicas.</param>
    /// <param name="obtener">Consulta de una practica.</param>
    /// <param name="crear">Alta de practica.</param>
    /// <param name="actualizar">Reasignacion de participantes y cambio de modalidad.</param>
    /// <param name="cambiarEstado">Transicion dentro del ciclo de vida.</param>
    public PracticasController(
        ListarPracticasUseCase listar,
        ObtenerPracticaUseCase obtener,
        CrearPracticaUseCase crear,
        ActualizarPracticaUseCase actualizar,
        CambiarEstadoPracticaUseCase cambiarEstado)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _actualizar = actualizar;
        _cambiarEstado = cambiarEstado;
    }

    /// <summary>
    /// Lista las practicas visibles para el solicitante, con filtros opcionales.
    /// </summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <param name="programaId">Programa por el que filtrar. Se omite para no filtrar.</param>
    /// <param name="fichaId">Ficha por la que filtrar. Se omite para no filtrar.</param>
    /// <param name="estado">Estado por el que filtrar. Se omite para no filtrar.</param>
    /// <returns>Coleccion de practicas.</returns>
    /// <remarks>
    /// Los filtros se aplican dentro del alcance ya restringido: uno que apunte
    /// fuera de el devuelve una lista vacia con 200, no 403 (H19).
    /// </remarks>
    /// <response code="200">Listado de practicas.</response>
    /// <response code="403">El rol del token no es uno de los tres del sistema.</response>
    /// <response code="422">El estado indicado no es uno de los cuatro del ciclo de vida.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PracticaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Listar(
        CancellationToken ct,
        [FromQuery] int? programaId = null,
        [FromQuery] int? fichaId = null,
        [FromQuery] string? estado = null) =>
        Ok(await _listar.ExecuteAsync(programaId, fichaId, estado, ct));

    /// <summary>Obtiene una practica por su identificador.</summary>
    /// <param name="id">Identificador de la practica.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Datos de la practica.</returns>
    /// <response code="200">Practica encontrada.</response>
    /// <response code="403">La practica queda fuera del alcance del solicitante.</response>
    /// <response code="404">La practica no existe.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PracticaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct) =>
        Ok(await _obtener.ExecuteAsync(id, ct));

    /// <summary>Da de alta una practica productiva.</summary>
    /// <param name="request">Datos de la practica a crear.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La practica creada.</returns>
    /// <response code="201">Practica creada.</response>
    /// <response code="400">Los datos no superan la validacion de forma.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="409">El aprendiz ya tiene una practica sin finalizar.</response>
    /// <response code="422">
    /// La ficha o la empresa no existen, los participantes no tienen el rol o el
    /// estado esperados, o la modalidad es incoherente con la empresa.
    /// </response>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(PracticaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Crear(CrearPracticaRequest request, CancellationToken ct)
    {
        var creada = await _crear.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    /// <summary>
    /// Reasigna los participantes de una practica y cambia su modalidad. No
    /// edita fechas (H29).
    /// </summary>
    /// <param name="id">Practica a modificar.</param>
    /// <param name="request">Participantes y modalidad nuevos.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La practica actualizada.</returns>
    /// <response code="200">Practica actualizada.</response>
    /// <response code="400">Los datos no superan la validacion de forma.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">La practica no existe.</response>
    /// <response code="409">El aprendiz nuevo ya tiene una practica sin finalizar.</response>
    /// <response code="422">
    /// La empresa no existe, los participantes no tienen el rol o el estado
    /// esperados, o la modalidad es incoherente con la empresa.
    /// </response>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(PracticaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(
        int id, ActualizarPracticaRequest request, CancellationToken ct) =>
        Ok(await _actualizar.ExecuteAsync(id, request, ct));

    /// <summary>
    /// Mueve la practica dentro del ciclo de vida de RN-05, retroceso incluido.
    /// </summary>
    /// <param name="id">Practica afectada.</param>
    /// <param name="request">Estado destino y, si el destino es Finalizada, fecha de cierre.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La practica con su estado actualizado.</returns>
    /// <response code="200">Estado cambiado.</response>
    /// <response code="400">El estado no es uno de los cuatro del ciclo de vida.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">La practica no existe.</response>
    /// <response code="422">
    /// El estado destino coincide con el actual, o la fecha de cierre precede a
    /// la de inicio.
    /// </response>
    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(PracticaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CambiarEstado(
        int id, CambiarEstadoPracticaRequest request, CancellationToken ct) =>
        Ok(await _cambiarEstado.ExecuteAsync(id, request, ct));
}
