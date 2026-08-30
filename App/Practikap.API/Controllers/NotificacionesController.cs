using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Notificaciones;
using Practikap.Application.UseCases.Notificaciones;

namespace Practikap.API.Controllers;

/// <summary>
/// Notificaciones internas. Modulo M6, CU-06 (RF-07, RN-09, RN-13).
/// </summary>
/// <remarks>
/// Tres endpoints (L1): la bandeja del solicitante, la emision administrativa y
/// la marca de lectura. La Matriz_de_Roles hoja 3 lista los dos primeros y no el
/// PATCH, aunque CU-06 y HU-09 describen que las notificaciones se marcan como
/// leidas al abrirlas. La divergencia queda como FA-32 documental.
///
/// Cierra el modulo M6, que el Doc_Arquitectura 7.1 reparte entre el paso 4.5,
/// que fue la mensajeria, y este.
///
/// No expone DELETE, que es la decision F3, ni PUT: una notificacion emitida no se
/// edita. Lo unico que cambia despues del alta es su marca de lectura, y va en
/// sub-recurso propio, con la misma forma que K6 le dio al mensaje.
///
/// La mayoria de las notificaciones no nace de ninguno de estos tres endpoints,
/// sino de los eventos de M4, M5 y la mensajeria, que las emiten por
/// IGeneradorDeNotificaciones dentro de la transaccion del evento (L5, L6). El
/// POST de aca es solo el canal manual del Administrador.
/// </remarks>
[ApiController]
[Route("api/notificaciones")]
[Authorize]
public sealed class NotificacionesController : ControllerBase
{
    private readonly ListarNotificacionesUseCase _listar;
    private readonly CrearNotificacionAdministrativaUseCase _emitir;
    private readonly MarcarNotificacionLeidaUseCase _marcarLeida;

    /// <summary>Crea el controlador.</summary>
    /// <param name="listar">Bandeja del solicitante.</param>
    /// <param name="emitir">Emision administrativa.</param>
    /// <param name="marcarLeida">Marca de lectura.</param>
    public NotificacionesController(
        ListarNotificacionesUseCase listar,
        CrearNotificacionAdministrativaUseCase emitir,
        MarcarNotificacionLeidaUseCase marcarLeida)
    {
        _listar = listar;
        _emitir = emitir;
        _marcarLeida = marcarLeida;
    }

    /// <summary>Devuelve las notificaciones del solicitante.</summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <param name="soloNoLeidas">true para obtener unicamente las pendientes de lectura. Se omite para obtener todas.</param>
    /// <returns>Sus notificaciones, de la mas reciente a la mas antigua.</returns>
    /// <remarks>
    /// El CancellationToken va primero porque el filtro es opcional: C# no admite
    /// un parametro obligatorio despues de uno con valor por defecto
    /// (Doc_Tecnico 5.8). Misma forma que UsuariosController.Listar.
    ///
    /// Abierto a los tres roles, y cada uno ve solo las suyas (L3). No lleva
    /// [Authorize(Roles = ...)] porque no hay rol que excluir, y a diferencia de
    /// todos los demas listados del sistema tampoco tiene alcance de supervision:
    /// el Administrador ve su propia bandeja y ninguna ajena. Es donde L3 diverge
    /// a proposito de K4, que si le daba alcance global sobre los hilos de
    /// mensajes.
    ///
    /// De ahi que no declare 403. El identificador del destinatario no viaja en
    /// ningun parametro —sale del token—, de modo que no existe consulta ajena que
    /// rechazar y el peor caso es una lista vacia.
    /// </remarks>
    /// <response code="200">Bandeja del solicitante, posiblemente vacia.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificacionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken ct, [FromQuery] bool soloNoLeidas = false) =>
        Ok(await _listar.ExecuteAsync(soloNoLeidas, ct));

    /// <summary>Emite una notificacion administrativa dirigida a un usuario.</summary>
    /// <param name="request">Destinatario y contenido.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La notificacion emitida.</returns>
    /// <remarks>
    /// Exclusivo del Administrador, como fija la Matriz_de_Roles hoja 3. El
    /// Instructor y el Aprendiz no emiten notificaciones por ninguna via directa:
    /// las suyas nacen de los eventos de M4, M5 y la mensajeria.
    ///
    /// El cuerpo no acepta el tipo: lo fija el caso de uso en 'Administrativa',
    /// que es el valor que L2 agrego al ENUM justo para este endpoint. Tampoco
    /// acepta regla_id, que solo puebla el Motor de Reglas junto al tipo 'Riesgo'
    /// (RN-09), ni la fecha, que la determina el servidor.
    ///
    /// Responde 201 <b>sin cabecera Location</b>, y es una divergencia deliberada
    /// respecto de los POST de M3, M5 y 4.5, que devuelven CreatedAtAction. No hay
    /// a donde apuntar: M6 no abre un GET de notificacion individual, y el GET del
    /// listado devuelve la bandeja del solicitante, que por L3 nunca contiene la
    /// notificacion que el Administrador acaba de emitir para otro. Una cabecera
    /// hacia /api/notificaciones seria una direccion donde el emisor no va a
    /// encontrar lo que creo.
    /// </remarks>
    /// <response code="201">Notificacion emitida.</response>
    /// <response code="400">Los datos no superan la validacion de forma, o el contenido esta vacio o excede los 255 caracteres.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="422">El usuario destinatario no existe.</response>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(NotificacionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Emitir(CrearNotificacionRequest request, CancellationToken ct)
    {
        var emitida = await _emitir.ExecuteAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, emitida);
    }

    /// <summary>Marca una notificacion como leida por su destinatario.</summary>
    /// <param name="id">Notificacion a marcar.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La notificacion, ya con su marca.</returns>
    /// <remarks>
    /// Sub-recurso propio y no un PATCH sobre la notificacion entera, con la misma
    /// forma que K6 le dio al mensaje: la marca de lectura es el unico estado
    /// alterable y no admite cuerpo, porque quien marca sale del token.
    ///
    /// Solo el destinatario (L4). No lleva restriccion por rol a proposito: su
    /// puerta no es el rol sino ser el destinatario de esa notificacion, y eso
    /// solo se sabe con la notificacion cargada (ADR-03). El Administrador que la
    /// emitio tampoco puede marcarla: emitirla no le da derecho a darla por leida
    /// en nombre de otro.
    ///
    /// Es idempotente. Un segundo PATCH sobre la misma notificacion devuelve 200
    /// con la misma marca, y no el 422 con el que responden las anulaciones de M4
    /// y M5: aquellas son irreversibles, esta es la misma intencion repetida. Es
    /// el criterio de K9 aplicado a L4.
    /// </remarks>
    /// <response code="200">Notificacion marcada como leida, o ya lo estaba.</response>
    /// <response code="403">El solicitante no es el destinatario de la notificacion.</response>
    /// <response code="404">La notificacion no existe.</response>
    [HttpPatch("{id:int}/leida")]
    [ProducesResponseType(typeof(NotificacionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarLeida(int id, CancellationToken ct) =>
        Ok(await _marcarLeida.ExecuteAsync(id, ct));
}
