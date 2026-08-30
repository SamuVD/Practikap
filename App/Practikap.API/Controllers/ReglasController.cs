using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Reglas;
using Practikap.Application.UseCases.Reglas;

namespace Practikap.API.Controllers;

/// <summary>
/// Motor de Reglas Dinamicas. Modulo M2, CU-02 (RF-10, RN-06, RN-07, RN-08,
/// RN-09).
/// </summary>
/// <remarks>
/// Es el componente diferenciador de Practikap: estos cinco endpoints son el
/// unico camino por el que el comportamiento de la plataforma cambia <b>sin tocar
/// el codigo fuente y sin desplegar de nuevo</b>, que es lo que RN-08 exige y lo
/// que HU-10 describe.
///
/// [Authorize(Roles = "Administrador")] va a nivel de clase y no accion por accion,
/// a diferencia de PracticasController y NotificacionesController. Aqui no hay nada
/// que repartir: la Matriz_de_Roles hoja 2 le da al Administrador alcance Total
/// sobre M2 y deja a Instructor y Aprendiz sin acceso alguno, incluida la lectura.
/// La segunda barrera vive igual en los cinco casos de uso, sobre IContextoUsuario
/// (ADR-03).
///
/// La Matriz_de_Roles hoja 3 no lista estos cinco endpoints: da las rutas genericas
/// /api/reglas y PATCH /api/reglas/{id}. N6 fija los verbos concretos y mueve el
/// PATCH a un sub-recurso propio. La divergencia queda como FA-33 documental.
///
/// No expone DELETE (decision F3). Una regla se retira con el PATCH y conserva la
/// traza de las notificaciones que origino; ademas fk_notificaciones_regla es
/// ON DELETE RESTRICT, de modo que una regla que ya disparo alertas no podria
/// borrarse aunque el endpoint existiera.
///
/// <b>Este controlador no evalua nada.</b> Configura las reglas y nada mas. Quien
/// las aplica es MotorDeReglas, en el Dominio, que la Ronda 2 conecta a los
/// enganches de M4 y M5 (RN-06, RN-09).
/// </remarks>
[ApiController]
[Route("api/reglas")]
[Authorize(Roles = "Administrador")]
public sealed class ReglasController : ControllerBase
{
    private readonly ListarReglasUseCase _listar;
    private readonly ObtenerReglaUseCase _obtener;
    private readonly CrearReglaUseCase _crear;
    private readonly ActualizarReglaUseCase _actualizar;
    private readonly CambiarActivaReglaUseCase _cambiarActiva;

    /// <summary>Crea el controlador.</summary>
    /// <param name="listar">Listado de reglas configuradas.</param>
    /// <param name="obtener">Consulta de una regla.</param>
    /// <param name="crear">Alta de regla.</param>
    /// <param name="actualizar">Redefinicion de una regla existente.</param>
    /// <param name="cambiarActiva">Incorporacion o retiro de las evaluaciones.</param>
    public ReglasController(
        ListarReglasUseCase listar,
        ObtenerReglaUseCase obtener,
        CrearReglaUseCase crear,
        ActualizarReglaUseCase actualizar,
        CambiarActivaReglaUseCase cambiarActiva)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _actualizar = actualizar;
        _cambiarActiva = cambiarActiva;
    }

    /// <summary>Lista las reglas configuradas en el Motor.</summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Las reglas, en el orden en que el Motor las evaluaria.</returns>
    /// <remarks>
    /// Devuelve las activas y las inactivas: es el panel de administracion, y una
    /// regla retirada tiene que poder verse para poder volver a activarse (RN-08).
    ///
    /// El orden es prioridad ascendente, desempatada por identificador, que es el
    /// mismo con el que el Motor las recorre. Lo que se lee arriba es lo que se
    /// aplica primero (RN-07).
    ///
    /// Sin parametros de filtro: el catalogo de reglas de una institucion se cuenta
    /// en decenas, no en miles, y filtrarlo en el cliente es mas util que decidir
    /// aqui por que campos se puede preguntar.
    /// </remarks>
    /// <response code="200">Listado de reglas, posiblemente vacio.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReglaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await _listar.ExecuteAsync(ct));

    /// <summary>Obtiene una regla por su identificador.</summary>
    /// <param name="id">Identificador de la regla.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Datos de la regla.</returns>
    /// <response code="200">Regla encontrada.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">La regla no existe.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReglaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct) =>
        Ok(await _obtener.ExecuteAsync(id, ct));

    /// <summary>Da de alta una regla del Motor.</summary>
    /// <param name="request">Definicion de la regla a crear.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La regla creada.</returns>
    /// <remarks>
    /// La regla nace activa y participa en toda evaluacion posterior desde la
    /// confirmacion. No hay estado intermedio de borrador: si no debe regir todavia,
    /// se crea y se desactiva con el PATCH.
    ///
    /// El cuerpo no acepta el umbral, que el caso de uso escribe igual al valor de
    /// la condicion (N3); ni la activacion, que es del PATCH; ni el creador, que
    /// sale del token (RF-10).
    ///
    /// Responde 201 con cabecera Location hacia el GET individual, que es el patron
    /// de M3 y M5. Diverge del POST de notificaciones del paso 4.6, que no la
    /// llevaba, y por la razon contraria: alli no habia GET por identificador al que
    /// apuntar, y aqui si.
    /// </remarks>
    /// <response code="201">Regla creada.</response>
    /// <response code="400">Los datos no superan la validacion de forma, o el operador no es uno de los seis.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="422">El campo evaluado o la accion resultante quedan fuera de las listas que el Motor sabe tratar.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ReglaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Crear(CrearReglaRequest request, CancellationToken ct)
    {
        var creada = await _crear.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    /// <summary>Reemplaza la definicion de una regla existente.</summary>
    /// <param name="id">Regla a modificar.</param>
    /// <param name="request">Definicion nueva, completa.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La regla con su definicion actualizada.</returns>
    /// <remarks>
    /// Es el unico PUT de Practikap, y diverge a proposito de J3, que se lo nego a
    /// M5. Una calificacion es un registro historico y corregirla en el sitio
    /// borraria la traza que RN-12 exige; una regla es configuracion vigente, y
    /// RN-08 pide justamente poder ajustarla sin redespliegue. Obligar a retirarla y
    /// crear otra dejaria la tabla llena de versiones muertas y cambiaria el
    /// identificador al que apuntan las notificaciones ya emitidas (RN-09).
    ///
    /// <b>No cambia la activacion.</b> Editar una regla retirada la deja retirada, y
    /// editar una activa la deja activa con la definicion nueva ya rigiendo.
    /// Incorporarla o retirarla es el PATCH.
    /// </remarks>
    /// <response code="200">Regla actualizada.</response>
    /// <response code="400">Los datos no superan la validacion de forma, o el operador no es uno de los seis.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">La regla no existe.</response>
    /// <response code="422">El campo evaluado o la accion resultante quedan fuera de las listas que el Motor sabe tratar.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ReglaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(
        int id, ActualizarReglaRequest request, CancellationToken ct) =>
        Ok(await _actualizar.ExecuteAsync(id, request, ct));

    /// <summary>Incorpora una regla a las evaluaciones del Motor o la retira.</summary>
    /// <param name="id">Regla afectada.</param>
    /// <param name="request">Estado de activacion destino.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La regla con su activacion aplicada.</returns>
    /// <remarks>
    /// Sub-recurso propio y no un PATCH sobre la regla entera, con la misma forma
    /// que K6 le dio al mensaje y L4 a la notificacion: la activacion es una
    /// decision separada de la definicion, y mezclarlas en un mismo verbo dejaria
    /// sin distinguir «retirar esta regla» de «editarla».
    ///
    /// El efecto es inmediato: el Motor pide sus reglas en cada invocacion y no las
    /// guarda en memoria, de modo que la evaluacion siguiente ya cuenta con el
    /// cambio. Es la lectura literal de RN-08.
    ///
    /// Es idempotente. Un segundo PATCH con el mismo valor devuelve 200 con la misma
    /// marca, y no el 422 con el que responden las anulaciones de M4 y M5: aquellas
    /// son irreversibles, esta es reversible y es la misma intencion repetida. Es el
    /// criterio de K9 y L4 aplicado a M2.
    /// </remarks>
    /// <response code="200">Activacion aplicada, o la regla ya estaba asi.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">La regla no existe.</response>
    [HttpPatch("{id:int}/activa")]
    [ProducesResponseType(typeof(ReglaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarActiva(
        int id, CambiarActivaReglaRequest request, CancellationToken ct) =>
        Ok(await _cambiarActiva.ExecuteAsync(id, request, ct));
}
