using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Configuracion;
using Practikap.Application.UseCases.Configuracion;

namespace Practikap.API.Controllers;

/// <summary>
/// Configuracion general del sistema. Modulo M8, CU-08 (RF-09, RN-08).
/// </summary>
/// <remarks>
/// <b>El nombre de esta clase va en singular</b>, y es la unica excepcion a la
/// convencion de plural del Doc_Tecnico 5.3 despues de AuthController. La ruta es
/// api/configuracion porque asi la concede la Matriz_de_Roles hoja 3, y el recurso
/// tampoco es una coleccion de cosas contables sino <b>la</b> configuracion del
/// sistema, expuesta como pares clave/valor. Pluralizarlo daria
/// api/configuraciones, que es un recurso que la matriz no concede y que ademas
/// describe mal lo que hay detras.
///
/// [Authorize(Roles = "Administrador")] va a nivel de clase y no accion por accion,
/// con la misma forma que ReglasController. Aqui tampoco hay nada que repartir: los
/// tres endpoints admiten un solo rol, y todo M8 es del Administrador (P3). No hay
/// alcance de RN-13 que resolver porque solo hay uno vivo, Global. La segunda barrera
/// vive igual en los tres casos de uso, sobre IContextoUsuario (ADR-03).
///
/// La Matriz_de_Roles hoja 3 concede /api/programas y /api/configuracion para todo
/// M8, <b>sin verbos y sin mencionar la auditoria</b>. P7 fija cuatro endpoints
/// —estos tres mas el GET de la bitacora— y P1 deja /api/programas donde ya estaba,
/// en M3, porque M8 no reimplementa lo que cerraron los pasos 4.1, 4.2 y 4.7.
/// <b>La divergencia queda como FA-36 documental.</b>
///
/// No expone DELETE (decision F3). Una clave no se borra: se le establece otro
/// valor. El catalogo cerrado de P8 hace que la pregunta ni siquiera se plantee,
/// porque no puede haber entradas huerfanas que limpiar.
///
/// <b>Este controlador no decide nada.</b> Ni que claves existen, ni que valores
/// admiten, ni si el PUT crea o actualiza: eso son ReglasDeConfiguracion y
/// EstablecerConfiguracionUseCase.
/// </remarks>
[ApiController]
[Route("api/configuracion")]
[Authorize(Roles = "Administrador")]
public sealed class ConfiguracionController : ControllerBase
{
    private readonly ListarConfiguracionUseCase _listar;
    private readonly ObtenerConfiguracionUseCase _obtener;
    private readonly EstablecerConfiguracionUseCase _establecer;

    /// <summary>Crea el controlador.</summary>
    /// <param name="listar">Listado de entradas de configuracion.</param>
    /// <param name="obtener">Consulta de una entrada por su clave.</param>
    /// <param name="establecer">Establecimiento del valor de una clave.</param>
    public ConfiguracionController(
        ListarConfiguracionUseCase listar,
        ObtenerConfiguracionUseCase obtener,
        EstablecerConfiguracionUseCase establecer)
    {
        _listar = listar;
        _obtener = obtener;
        _establecer = establecer;
    }

    /// <summary>Lista las entradas de configuracion del sistema.</summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Las entradas, en orden alfabetico de clave.</returns>
    /// <remarks>
    /// Devuelve las entradas <b>persistidas</b>, no el catalogo de claves que el
    /// sistema admite (P8). Una clave que nunca se establecio no tiene fila y no
    /// aparece: lo que se lista es lo que esta configurado, no lo que podria
    /// configurarse. En un sistema recien instalado la lista sale vacia.
    ///
    /// Sin parametros de filtro: el catalogo tiene dos claves y crecera en unidades,
    /// no en miles.
    /// </remarks>
    /// <response code="200">Listado de entradas, posiblemente vacio.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ConfiguracionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await _listar.ExecuteAsync(ct));

    /// <summary>Obtiene una entrada de configuracion por su clave.</summary>
    /// <param name="clave">Clave de configuracion, por ejemplo estado_practica_por_defecto.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La entrada, con su valor vigente y su responsable.</returns>
    /// <remarks>
    /// <b>La clave viaja como texto y la plantilla no lleva restriccion :int</b>, a
    /// diferencia de todos los demas GET por identificador del sistema. Ponerla
    /// devolveria un 404 de enrutamiento antes de que la peticion llegara al caso de
    /// uso, y el 404 saldria sin el mensaje que explica que la clave no esta
    /// configurada.
    ///
    /// Una clave del catalogo que todavia no se establecio responde 404 igual que
    /// una inventada: los dos casos son el mismo hecho, no hay fila. Que la clave sea
    /// legitima no la convierte en un recurso existente.
    /// </remarks>
    /// <response code="200">Entrada encontrada.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">La clave no esta configurada.</response>
    [HttpGet("{clave}")]
    [ProducesResponseType(typeof(ConfiguracionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(string clave, CancellationToken ct) =>
        Ok(await _obtener.ExecuteAsync(clave, ct));

    /// <summary>Establece el valor de una clave, creando la entrada si no existia.</summary>
    /// <param name="clave">Clave de configuracion, una de las del catalogo de P8.</param>
    /// <param name="request">Valor a establecer.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La entrada resultante, con su valor ya aplicado.</returns>
    /// <remarks>
    /// <b>Responde 201 o 200, y la diferencia es observable.</b> El 201 lleva
    /// cabecera Location hacia el GET individual y significa que la entrada no
    /// existia y se creo; el 200, que ya existia y se le cambio el valor. Es la misma
    /// forma con la que el POST de reportes distingue el caso de O8, aunque no puede
    /// usar su truco de mirar el Id: ConfiguracionResponse no lo expone.
    ///
    /// Es un PUT y no un POST porque la clave es el identificador del recurso y viaja
    /// en la ruta: el cliente decide donde escribe. Es tambien el segundo PUT del
    /// proyecto, despues del de M2, y por la misma razon que aquel diverge de J3:
    /// esto es configuracion vigente, no historia. RN-08 pide justamente poder
    /// ajustarla sin tocar el codigo fuente ni desplegar de nuevo.
    ///
    /// <b>Es idempotente.</b> Un segundo PUT con el mismo valor devuelve 200 con la
    /// misma entrada, y no el 422 con el que responden las anulaciones de M4 y M5:
    /// aquellas son irreversibles, esta es la misma intencion repetida. Es el criterio
    /// de K9, L4 y N6 aplicado a M8.
    ///
    /// <b>Una clave fuera del catalogo responde 422 y no crea nada</b> (P8). El
    /// almacen es clave/valor pero no es abierto: si lo fuera, el panel se llenaria
    /// de entradas que ningun codigo lee.
    ///
    /// El cuerpo no acepta la clave, que va en la ruta; ni la descripcion, que sale
    /// del catalogo y solo se escribe al crear; ni el responsable, que sale del token
    /// (RF-09); ni la fecha, que la determina el servidor (RN-11).
    /// </remarks>
    /// <response code="201">La entrada no existia y se creo.</response>
    /// <response code="200">La entrada ya existia y se le establecio el valor.</response>
    /// <response code="400">El valor viene vacio.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="422">La clave no pertenece al catalogo del sistema, o el valor no es de los que esa clave admite.</response>
    [HttpPut("{clave}")]
    [ProducesResponseType(typeof(ConfiguracionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ConfiguracionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Establecer(
        string clave, EstablecerConfiguracionRequest request, CancellationToken ct)
    {
        var resultado = await _establecer.ExecuteAsync(clave, request, ct);

        return resultado.Creada
            ? CreatedAtAction(nameof(Obtener), new { clave = resultado.Entrada.Clave }, resultado.Entrada)
            : Ok(resultado.Entrada);
    }
}
