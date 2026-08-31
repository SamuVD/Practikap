using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Auditoria;
using Practikap.Application.UseCases.Auditoria;

namespace Practikap.API.Controllers;

/// <summary>
/// Bitacora de acciones sensibles del sistema. Modulo M8, CU-08 (RF-09, RN-01,
/// RN-05, RN-08, RN-12).
/// </summary>
/// <remarks>
/// Un solo endpoint, de solo lectura. La bitacora <b>no se escribe desde HTTP</b>:
/// sus asientos nacen dentro de la transaccion de la accion que los origina, por el
/// registrador que aporta la Ronda 2 del paso 4.9, con la misma forma que
/// IGeneradorDeNotificaciones (L6) y IEvaluadorDeReglas (N11). Un asiento que se
/// pudiera crear desde fuera no seria evidencia de nada.
///
/// [Authorize(Roles = "Administrador")] va a nivel de clase, con la misma forma que
/// ReglasController y ConfiguracionController: todo M8 es del Administrador (P3), con
/// un unico alcance vivo, Global. <b>La bitacora no se recorta por RN-13</b>, y no
/// podria: su razon de ser es que alguien vea lo que los demas hicieron. La segunda
/// barrera vive igual en el caso de uso, sobre IContextoUsuario (ADR-03).
///
/// La Matriz_de_Roles hoja 3 concede /api/programas y /api/configuracion para todo
/// M8 y <b>no menciona la auditoria en ninguna parte</b>, aunque el DDL declara la
/// tabla desde el principio y CU-08 describe el panel del Administrador. P7 fija
/// cuatro endpoints, y este es el cuarto. <b>La divergencia queda como FA-36
/// documental</b>, junto con la de ConfiguracionController.
///
/// No expone DELETE (decision F3), y en este modulo la razon es la mas clara de
/// todas: borrar un asiento destruye la evidencia que la bitacora existe para
/// conservar. Tampoco PUT ni PATCH, porque un asiento es inmutable una vez escrito.
///
/// <b>Hoy este endpoint devuelve vacio siempre, y es lo esperado.</b> Nada de la
/// Ronda 1 escribe en auditoria: los once puntos de enganche son la Ronda 2.
/// </remarks>
[ApiController]
[Route("api/auditoria")]
[Authorize(Roles = "Administrador")]
public sealed class AuditoriaController : ControllerBase
{
    private readonly ListarAuditoriaUseCase _listar;

    /// <summary>Crea el controlador.</summary>
    /// <param name="listar">Consulta de la bitacora.</param>
    public AuditoriaController(ListarAuditoriaUseCase listar) => _listar = listar;

    /// <summary>Consulta la bitacora de acciones sensibles.</summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <param name="entidadAfectada">Entidad por la que filtrar, como texto. Se omite para no filtrar.</param>
    /// <param name="accion">Tipo de accion por el que filtrar, como texto. Se omite para no filtrar.</param>
    /// <param name="usuarioId">Actor por el que filtrar. Se omite para no filtrar.</param>
    /// <param name="desde">Limite inferior del rango, inclusive. Se omite para no acotar.</param>
    /// <param name="hasta">Limite superior del rango, inclusive. Se omite para no acotar.</param>
    /// <returns>Los asientos que satisfacen el filtro, del mas reciente al mas antiguo.</returns>
    /// <remarks>
    /// El CancellationToken va primero porque los cinco criterios son opcionales: C#
    /// no admite un parametro obligatorio despues de uno con valor por defecto
    /// (Doc_Tecnico 5.8). Misma forma que UsuariosController.Listar y
    /// NotificacionesController.Listar.
    ///
    /// <b>Los cinco criterios se combinan con Y logico y viajan a MySQL</b> (P6), a
    /// diferencia de los nueve filtros de M7, que O4 resolvio en memoria: alli habia
    /// un listado de alcance previo del que colgarse y las practicas se cuentan en
    /// cientos; aqui no hay listado previo y la tabla crece con cada accion sensible
    /// del sistema.
    ///
    /// La firma del repositorio cambio en este paso para admitirlos. La del
    /// scaffolding exigia desde y hasta <b>obligatorios</b>, de modo que no habia
    /// manera de pedir la bitacora entera, y no admitia filtrar por actor, que es la
    /// primera pregunta que un panel de auditoria hace.
    ///
    /// Los dos filtros de enumerado se escriben con <b>el nombre del miembro</b>
    /// —Practicas, RetrocesoEstado—, no con el literal que guarda la columna
    /// —"practicas", "Retroceso_estado"— (H31). Es el mismo texto que la respuesta
    /// devuelve: lo que se lee se puede reenviar sin traducir.
    ///
    /// Sin paginacion ni limite. La bitacora de la institucion en el alcance de v1 se
    /// cuenta en miles de filas, y quien necesite acotarla tiene cinco criterios para
    /// hacerlo, empezando por el rango de fechas.
    ///
    /// Un filtro que no encuentra nada devuelve 200 con lista vacia, no 404: un
    /// filtro sin resultados es una respuesta, no un error del solicitante (O8).
    /// </remarks>
    /// <response code="200">Asientos que satisfacen el filtro, posiblemente ninguno.</response>
    /// <response code="400">El rango de fechas esta invertido.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="422">La entidad afectada o la accion traen un literal desconocido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RegistroAuditoriaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Listar(
        CancellationToken ct,
        [FromQuery] string? entidadAfectada = null,
        [FromQuery] string? accion = null,
        [FromQuery] int? usuarioId = null,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null) =>
        Ok(await _listar.ExecuteAsync(entidadAfectada, accion, usuarioId, desde, hasta, ct));
}
