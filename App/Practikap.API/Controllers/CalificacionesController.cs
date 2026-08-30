using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Calificaciones;
using Practikap.Application.UseCases.Calificaciones;

namespace Practikap.API.Controllers;

/// <summary>
/// Calificacion bidireccional. Modulo M5, CU-05 (RF-06, RN-10, RN-11, RN-12,
/// RN-13).
/// </summary>
/// <remarks>
/// Un solo controlador para las dos direcciones: los cinco endpoints cuelgan de
/// /api/calificaciones, que es un unico recurso de primer nivel. La direccion la
/// distingue el segmento siguiente —/instructor o /aprendiz—, no un controlador
/// aparte ni un campo del cuerpo.
///
/// No expone DELETE ni PUT, y aqui las dos ausencias pesan. La primera es la
/// decision F3. La segunda es J3: la Matriz_de_Roles y el Doc_Arquitectura
/// listan un PUT /api/calificaciones/{id} para que el instructor califique, pero
/// una calificacion es un registro historico y RN-12 lo hace inmutable. Lo que
/// alli figura como actualizacion es en realidad un alta, y por eso es un POST.
/// La divergencia con la Matriz_de_Roles queda como FA-30 documental.
///
/// Los dos POST llevan el rol del emisor y los dos PATCH, Administrador, que es
/// la lectura de la columna Supervision de la Matriz_de_Roles hoja 3. La lectura
/// no lleva restriccion por rol: los tres alcances de RN-13 los resuelve el caso
/// de uso sobre IContextoUsuario (ADR-03).
/// </remarks>
[ApiController]
[Route("api/calificaciones")]
[Authorize]
public sealed class CalificacionesController : ControllerBase
{
    private readonly RegistrarCalificacionInstructorUseCase _registrarDelInstructor;
    private readonly RegistrarCalificacionAprendizUseCase _registrarDelAprendiz;
    private readonly ListarCalificacionesDePracticaUseCase _listar;
    private readonly AnularCalificacionInstructorUseCase _anularDelInstructor;
    private readonly AnularCalificacionAprendizUseCase _anularDelAprendiz;

    /// <summary>Crea el controlador.</summary>
    /// <param name="registrarDelInstructor">Alta de la calificacion del instructor al aprendiz.</param>
    /// <param name="registrarDelAprendiz">Alta de la evaluacion del aprendiz al instructor.</param>
    /// <param name="listar">Consulta de las dos direcciones de una practica.</param>
    /// <param name="anularDelInstructor">Marca de anulacion sobre la direccion del instructor.</param>
    /// <param name="anularDelAprendiz">Marca de anulacion sobre la direccion del aprendiz.</param>
    public CalificacionesController(
        RegistrarCalificacionInstructorUseCase registrarDelInstructor,
        RegistrarCalificacionAprendizUseCase registrarDelAprendiz,
        ListarCalificacionesDePracticaUseCase listar,
        AnularCalificacionInstructorUseCase anularDelInstructor,
        AnularCalificacionAprendizUseCase anularDelAprendiz)
    {
        _registrarDelInstructor = registrarDelInstructor;
        _registrarDelAprendiz = registrarDelAprendiz;
        _listar = listar;
        _anularDelInstructor = anularDelInstructor;
        _anularDelAprendiz = anularDelAprendiz;
    }

    /// <summary>Registra la calificacion que el Instructor emite sobre el Aprendiz.</summary>
    /// <param name="request">Practica, valor y comentario.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La calificacion creada.</returns>
    /// <remarks>
    /// La fecha del registro no se envia ni se acepta: la determina el servidor
    /// (RN-11). Se admiten varias calificaciones sobre la misma practica (J5).
    ///
    /// La cabecera Location apunta al listado de la practica, que es donde la
    /// calificacion queda visible: M5 no abre un GET de calificacion individual.
    /// </remarks>
    /// <response code="201">Calificacion registrada.</response>
    /// <response code="400">Los datos no superan la validacion de forma, o el valor esta fuera de 0.0 a 5.0.</response>
    /// <response code="403">El rol autenticado no es Instructor, o la practica no es suya.</response>
    /// <response code="422">La practica no existe, o no esta En curso ni En riesgo.</response>
    [HttpPost("instructor")]
    [Authorize(Roles = "Instructor")]
    [ProducesResponseType(typeof(CalificacionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegistrarDelInstructor(
        CrearCalificacionRequest request, CancellationToken ct)
    {
        var creada = await _registrarDelInstructor.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(Listar), new { practicaId = creada.PracticaId }, creada);
    }

    /// <summary>Registra la evaluacion que el Aprendiz emite sobre el Instructor.</summary>
    /// <param name="request">Practica, valor y comentario.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La evaluacion creada.</returns>
    /// <remarks>
    /// Es un registro independiente del de la direccion contraria: no exige que
    /// el instructor haya calificado antes ni lo condiciona despues (RN-10).
    /// </remarks>
    /// <response code="201">Evaluacion registrada.</response>
    /// <response code="400">Los datos no superan la validacion de forma, o el valor esta fuera de 0.0 a 5.0.</response>
    /// <response code="403">El rol autenticado no es Aprendiz, o la practica no es suya.</response>
    /// <response code="422">La practica no existe, o no esta En curso ni En riesgo.</response>
    [HttpPost("aprendiz")]
    [Authorize(Roles = "Aprendiz")]
    [ProducesResponseType(typeof(CalificacionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegistrarDelAprendiz(
        CrearCalificacionRequest request, CancellationToken ct)
    {
        var creada = await _registrarDelAprendiz.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(Listar), new { practicaId = creada.PracticaId }, creada);
    }

    /// <summary>Devuelve las calificaciones de una practica en sus dos direcciones.</summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <param name="practicaId">Practica cuyas calificaciones se consultan.</param>
    /// <returns>Las dos listas y los dos promedios vigentes.</returns>
    /// <remarks>
    /// El CancellationToken va primero y el parametro de consulta despues, que es
    /// la forma que fija el Doc_Tecnico 5.8 para los endpoints con [FromQuery] y
    /// la misma que usa PracticasController.Listar.
    ///
    /// Devuelve tambien las anuladas, con su marca. Los promedios las excluyen
    /// (J5).
    /// </remarks>
    /// <response code="200">Calificaciones de la practica, posiblemente vacias.</response>
    /// <response code="403">La practica queda fuera del alcance del solicitante.</response>
    /// <response code="404">La practica no existe.</response>
    [HttpGet]
    [ProducesResponseType(typeof(CalificacionesDePracticaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Listar(CancellationToken ct, [FromQuery] int practicaId) =>
        Ok(await _listar.ExecuteAsync(practicaId, ct));

    /// <summary>Marca como anulada una calificacion emitida por el Instructor.</summary>
    /// <param name="id">Calificacion a anular.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La calificacion, con su marca y el identificador del anulador.</returns>
    /// <remarks>
    /// No borra ni edita: aplica la unica alteracion que RN-12 permite. El
    /// registro sigue apareciendo en el listado con su marca, y lo que cambia es
    /// el promedio vigente de su direccion, que deja de contarlo.
    ///
    /// No arrastra a la direccion contraria (RN-10) y no lleva cuerpo: el actor
    /// sale del token, no de la peticion.
    /// </remarks>
    /// <response code="200">Calificacion anulada.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">La calificacion no existe.</response>
    /// <response code="422">La calificacion ya se encontraba anulada.</response>
    [HttpPatch("instructor/{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(CalificacionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AnularDelInstructor(int id, CancellationToken ct) =>
        Ok(await _anularDelInstructor.ExecuteAsync(id, ct));

    /// <summary>Marca como anulada una evaluacion emitida por el Aprendiz.</summary>
    /// <param name="id">Evaluacion a anular.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La evaluacion, con su marca y el identificador del anulador.</returns>
    /// <remarks>
    /// Ruta propia y no un id compartido con la direccion contraria: son dos
    /// tablas separadas y los identificadores de una y otra no forman una sola
    /// serie, de modo que un PATCH /api/calificaciones/{id} seria ambiguo.
    /// </remarks>
    /// <response code="200">Evaluacion anulada.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">La evaluacion no existe.</response>
    /// <response code="422">La evaluacion ya se encontraba anulada.</response>
    [HttpPatch("aprendiz/{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(CalificacionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AnularDelAprendiz(int id, CancellationToken ct) =>
        Ok(await _anularDelAprendiz.ExecuteAsync(id, ct));
}
