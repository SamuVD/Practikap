using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Observaciones;
using Practikap.Application.DTOs.Seguimientos;
using Practikap.Application.UseCases.Observaciones;
using Practikap.Application.UseCases.Seguimientos;

namespace Practikap.API.Controllers;

/// <summary>
/// Registro y consulta de seguimientos. Modulo M4, CU-04 (RF-04, RF-05, RN-11,
/// RN-12, RN-13).
/// </summary>
/// <remarks>
/// No expone DELETE, y aqui la ausencia pesa mas que en M3: el historial es
/// inmutable por RN-12 y ni siquiera el Administrador borra. Lo unico que puede
/// hacer es marcar un registro como anulado, que es el PATCH.
///
/// Tampoco existe un PUT ni un PATCH que edite avance, etapa o contenido. Esa
/// ausencia es la forma en que la capa API sostiene RN-12: no hay verbo que
/// pudiera modificar un registro, y por eso no hay nada que autorizar.
///
/// Los dos POST llevan [Authorize(Roles = "Instructor")] y el PATCH,
/// Administrador, siguiendo la Matriz_de_Roles hoja 3. Las dos lecturas no
/// llevan restriccion por rol: los tres alcances de RN-13 los resuelve el caso
/// de uso sobre IContextoUsuario (ADR-03).
/// </remarks>
[ApiController]
[Route("api/seguimientos")]
[Authorize]
public sealed class SeguimientosController : ControllerBase
{
    private readonly RegistrarSeguimientoUseCase _registrar;
    private readonly ObtenerSeguimientoUseCase _obtener;
    private readonly AnularSeguimientoUseCase _anular;
    private readonly ListarSeguimientosDePracticaUseCase _listarDePractica;
    private readonly RegistrarObservacionUseCase _registrarObservacion;

    /// <summary>Crea el controlador.</summary>
    /// <param name="registrar">Alta de seguimiento.</param>
    /// <param name="obtener">Consulta de un seguimiento.</param>
    /// <param name="anular">Marca de anulacion.</param>
    /// <param name="listarDePractica">Historial de una practica.</param>
    /// <param name="registrarObservacion">Alta de observacion sobre un seguimiento.</param>
    public SeguimientosController(
        RegistrarSeguimientoUseCase registrar,
        ObtenerSeguimientoUseCase obtener,
        AnularSeguimientoUseCase anular,
        ListarSeguimientosDePracticaUseCase listarDePractica,
        RegistrarObservacionUseCase registrarObservacion)
    {
        _registrar = registrar;
        _obtener = obtener;
        _anular = anular;
        _listarDePractica = listarDePractica;
        _registrarObservacion = registrarObservacion;
    }

    /// <summary>Registra un seguimiento sobre una practica.</summary>
    /// <param name="request">Practica, avance y etapa.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El seguimiento creado.</returns>
    /// <remarks>
    /// La fecha del registro no se envia ni se acepta: la determina el servidor
    /// (RN-11).
    /// </remarks>
    /// <response code="201">Seguimiento registrado.</response>
    /// <response code="400">Los datos no superan la validacion de forma.</response>
    /// <response code="403">El rol autenticado no es Instructor, o la practica no es suya.</response>
    /// <response code="422">La practica no existe, o no esta En curso ni En riesgo.</response>
    [HttpPost]
    [Authorize(Roles = "Instructor")]
    [ProducesResponseType(typeof(SeguimientoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Registrar(CrearSeguimientoRequest request, CancellationToken ct)
    {
        var creado = await _registrar.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>Obtiene un seguimiento por su identificador.</summary>
    /// <param name="id">Identificador del seguimiento.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El seguimiento, con sus observaciones anidadas.</returns>
    /// <response code="200">Seguimiento encontrado.</response>
    /// <response code="403">El seguimiento queda fuera del alcance del solicitante.</response>
    /// <response code="404">El seguimiento no existe.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SeguimientoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct) =>
        Ok(await _obtener.ExecuteAsync(id, ct));

    /// <summary>Marca un seguimiento como anulado.</summary>
    /// <param name="id">Seguimiento a anular.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El seguimiento, con su marca y el identificador del anulador.</returns>
    /// <remarks>
    /// No borra ni edita: aplica la unica alteracion del historial que RN-12
    /// permite. El registro sigue apareciendo en el historial con su marca (I4),
    /// y sus observaciones conservan la suya, que no se propaga (I11).
    ///
    /// No lleva cuerpo: el actor sale del token, no de la peticion.
    /// </remarks>
    /// <response code="200">Seguimiento anulado.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">El seguimiento no existe.</response>
    /// <response code="422">El seguimiento ya se encontraba anulado.</response>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(SeguimientoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Anular(int id, CancellationToken ct) =>
        Ok(await _anular.ExecuteAsync(id, ct));

    /// <summary>Registra una observacion sobre un seguimiento.</summary>
    /// <param name="id">Seguimiento al que se asocia la observacion.</param>
    /// <param name="request">Contenido de la observacion.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La observacion creada.</returns>
    /// <remarks>
    /// La cabecera Location apunta al seguimiento y no a la observacion: es
    /// donde queda visible, anidada en el historial (I5). La v1 no abre un GET de
    /// observacion individual.
    /// </remarks>
    /// <response code="201">Observacion registrada.</response>
    /// <response code="400">Los datos no superan la validacion de forma.</response>
    /// <response code="403">El rol autenticado no es Instructor, o la practica no es suya.</response>
    /// <response code="404">El seguimiento no existe.</response>
    /// <response code="422">
    /// La practica no esta En curso ni En riesgo, o el seguimiento esta anulado.
    /// </response>
    [HttpPost("{id:int}/observaciones")]
    [Authorize(Roles = "Instructor")]
    [ProducesResponseType(typeof(ObservacionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegistrarObservacion(
        int id, CrearObservacionRequest request, CancellationToken ct)
    {
        var creada = await _registrarObservacion.ExecuteAsync(id, request, ct);
        return CreatedAtAction(nameof(Obtener), new { id }, creada);
    }

    /// <summary>Devuelve el historial de seguimientos de una practica.</summary>
    /// <param name="id">Practica cuyo historial se consulta.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Los seguimientos de la practica, con sus observaciones anidadas.</returns>
    /// <remarks>
    /// La ruta es absoluta y cuelga de /api/practicas, pero la accion vive en
    /// este controlador y no en PracticasController (I6): el recurso que devuelve
    /// es el seguimiento, y es aqui donde estan los casos de uso que lo conocen.
    ///
    /// Devuelve cada seguimiento con sus observaciones dentro, en una sola
    /// consulta (I5), y no oculta los anulados (I4).
    /// </remarks>
    /// <response code="200">Historial de la practica, posiblemente vacio.</response>
    /// <response code="403">La practica queda fuera del alcance del solicitante.</response>
    /// <response code="404">La practica no existe.</response>
    [HttpGet("/api/practicas/{id:int}/seguimientos")]
    [ProducesResponseType(typeof(IReadOnlyList<SeguimientoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarDePractica(int id, CancellationToken ct) =>
        Ok(await _listarDePractica.ExecuteAsync(id, ct));
}
