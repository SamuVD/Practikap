using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Mensajes;
using Practikap.Application.UseCases.Mensajes;

namespace Practikap.API.Controllers;

/// <summary>
/// Mensajeria interna. Modulo M6, CU-06 (RF-07, RN-13).
/// </summary>
/// <remarks>
/// Tres endpoints (K1): el envio, el hilo de una practica y la marca de lectura.
/// La Matriz_de_Roles hoja 3 solo lista el primero, aunque sus hojas 2 y 4 ya
/// conceden lectura de mensajeria al Administrador sin dar endpoint con el que
/// ejercerla. La divergencia queda como FA-31 documental.
///
/// No expone DELETE, que es la decision F3, ni PUT: un mensaje enviado no se
/// edita. Lo unico que cambia despues del alta es su marca de lectura, y va en
/// sub-recurso propio (K6) en lugar de en un PATCH sobre el mensaje entero,
/// porque es el unico estado que el destinatario puede alterar.
///
/// El POST lleva los dos roles que participan de una practica. El PATCH no lleva
/// restriccion por rol a proposito: su puerta no es el rol sino ser el
/// destinatario de ese mensaje, y eso solo se sabe con el mensaje cargado
/// (ADR-03, K5). El GET tampoco la lleva: los tres alcances de RN-13 los resuelve
/// el caso de uso.
///
/// La notificacion que RF-07 describe no nace en este controlador, pero si en el
/// caso de uso del envio desde el paso 4.6, que cableo el enganche de K7 (L5).
/// Su lectura y su marca viven en NotificacionesController.
/// </remarks>
[ApiController]
[Route("api/mensajes")]
[Authorize]
public sealed class MensajesController : ControllerBase
{
    private readonly EnviarMensajeUseCase _enviar;
    private readonly ListarMensajesDePracticaUseCase _listarDePractica;
    private readonly MarcarMensajeLeidoUseCase _marcarLeido;

    /// <summary>Crea el controlador.</summary>
    /// <param name="enviar">Envio de un mensaje.</param>
    /// <param name="listarDePractica">Hilo de una practica.</param>
    /// <param name="marcarLeido">Marca de lectura.</param>
    public MensajesController(
        EnviarMensajeUseCase enviar,
        ListarMensajesDePracticaUseCase listarDePractica,
        MarcarMensajeLeidoUseCase marcarLeido)
    {
        _enviar = enviar;
        _listarDePractica = listarDePractica;
        _marcarLeido = marcarLeido;
    }

    /// <summary>Envia un mensaje dentro del contexto de una practica.</summary>
    /// <param name="request">Practica y contenido.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El mensaje enviado.</returns>
    /// <remarks>
    /// El cuerpo no acepta emisor ni receptor: el primero sale del token y el
    /// segundo se deriva como el otro participante de la practica (K2). La fecha
    /// de envio tampoco se envia ni se acepta, la determina el servidor.
    ///
    /// El Administrador queda fuera: lee los hilos pero no escribe en ellos (K4).
    ///
    /// La cabecera Location apunta al hilo de la practica, que es donde el mensaje
    /// queda visible. M6 no abre un GET de mensaje individual.
    /// </remarks>
    /// <response code="201">Mensaje enviado.</response>
    /// <response code="400">Los datos no superan la validacion de forma, o el contenido esta vacio o excede el tope.</response>
    /// <response code="403">El rol autenticado no participa de la practica, o no comparte una practica activa con el receptor.</response>
    /// <response code="422">La practica no existe, o no esta En curso ni En riesgo.</response>
    [HttpPost]
    [Authorize(Roles = "Instructor,Aprendiz")]
    [ProducesResponseType(typeof(MensajeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Enviar(EnviarMensajeRequest request, CancellationToken ct)
    {
        var enviado = await _enviar.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(ListarDePractica), new { id = enviado.PracticaId }, enviado);
    }

    /// <summary>Devuelve el hilo de mensajes de una practica.</summary>
    /// <param name="id">Practica cuyo hilo se consulta.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Los mensajes de la practica, del mas antiguo al mas reciente.</returns>
    /// <remarks>
    /// La ruta es absoluta y cuelga de /api/practicas, pero la accion vive en este
    /// controlador y no en PracticasController (I6): el recurso que devuelve es el
    /// mensaje, y es aqui donde esta el caso de uso que lo conoce. Misma forma que
    /// el historial de seguimientos de M4.
    ///
    /// Se permite sobre practicas en cualquier estado, a diferencia del envio
    /// (K3), y el Administrador lo consulta con alcance de supervision (K4).
    ///
    /// El orden es ascendente: es un hilo de conversacion, no un historial de
    /// auditoria.
    /// </remarks>
    /// <response code="200">Hilo de la practica, posiblemente vacio.</response>
    /// <response code="403">La practica queda fuera del alcance del solicitante.</response>
    /// <response code="404">La practica no existe.</response>
    [HttpGet("/api/practicas/{id:int}/mensajes")]
    [ProducesResponseType(typeof(IReadOnlyList<MensajeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarDePractica(int id, CancellationToken ct) =>
        Ok(await _listarDePractica.ExecuteAsync(id, ct));

    /// <summary>Marca un mensaje como leido por su destinatario.</summary>
    /// <param name="id">Mensaje a marcar.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El mensaje, ya con su marca.</returns>
    /// <remarks>
    /// Sub-recurso propio y no un PATCH sobre el mensaje entero (K6): la marca de
    /// lectura es el unico estado alterable y no admite cuerpo, porque quien
    /// marca sale del token.
    ///
    /// Solo el destinatario (K5). El emisor marcando lo que el mismo escribio
    /// recibe 403, y el Administrador tambien: no es receptor de ningun mensaje.
    ///
    /// Es idempotente (K9). Un segundo PATCH sobre el mismo mensaje devuelve 200
    /// con la misma marca, y no el 422 con el que responden las anulaciones de M4
    /// y M5: aquellas son irreversibles, esta es la misma intencion repetida.
    /// </remarks>
    /// <response code="200">Mensaje marcado como leido, o ya lo estaba.</response>
    /// <response code="403">El solicitante no es el destinatario del mensaje.</response>
    /// <response code="404">El mensaje no existe.</response>
    [HttpPatch("{id:int}/leido")]
    [ProducesResponseType(typeof(MensajeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarLeido(int id, CancellationToken ct) =>
        Ok(await _marcarLeido.ExecuteAsync(id, ct));
}
