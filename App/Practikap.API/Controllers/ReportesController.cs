using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Reportes;
using Practikap.Application.UseCases.Reportes;

namespace Practikap.API.Controllers;

/// <summary>
/// Generacion, consulta y exportacion de reportes sobre practicas. Modulo M7,
/// CU-07 (RF-08, RN-13).
/// </summary>
/// <remarks>
/// Un reporte de Practikap es un <b>rastro</b>, no un archivo. Estos cuatro
/// endpoints registran que se consulto, con que criterios y quien, y recomponen el
/// contenido sobre las practicas vinculadas cada vez que se pide. El CSV que
/// descarga el cuarto tampoco se guarda: se formatea al vuelo sobre ese mismo
/// contenido recompuesto.
///
/// [Authorize(Roles = "Administrador,Instructor")] va a nivel de clase y no accion
/// por accion, con la misma forma que ReglasController. Aqui tampoco hay nada que
/// repartir: los cuatro endpoints admiten los mismos dos roles y ninguno admite al
/// Aprendiz, que queda fuera de M7 entero, incluida la lectura (O3). Lo que si
/// cambia entre los dos roles es el alcance de lo que ven, y eso lo resuelve cada
/// caso de uso sobre IContextoUsuario (ADR-03, RN-13): el Administrador tiene
/// alcance Global y el Instructor, Asignado. AlcanceConsulta.Propio no se usa en
/// este modulo.
///
/// La Matriz_de_Roles hoja 3 solo concede GET /api/reportes para todo M7. O1, O2 y
/// O21 fijan cuatro endpoints: el POST que genera y persiste, este GET que pasa a
/// listar el historico de lo generado, el GET por identificador que recompone el
/// contenido de uno y el GET de exportacion que lo entrega como CSV.
/// <b>La divergencia queda como FA-34 documental.</b>
///
/// No expone DELETE (decision F3), y en este modulo la razon es especialmente
/// clara: borrar un reporte destruiria la unica evidencia de que la consulta
/// ocurrio, que es exactamente lo que el modulo existe para conservar.
///
/// <b>Este controlador no calcula nada.</b> Ni filtra, ni promedia, ni decide que
/// practicas entran: eso son GenerarReporteUseCase y ArmadorDeReporte.
/// </remarks>
[ApiController]
[Route("api/reportes")]
[Authorize(Roles = "Administrador,Instructor")]
public sealed class ReportesController : ControllerBase
{
    private readonly GenerarReporteUseCase _generar;
    private readonly ListarReportesUseCase _listar;
    private readonly ObtenerReporteUseCase _obtener;
    private readonly ExportarReporteUseCase _exportar;

    /// <summary>Crea el controlador.</summary>
    /// <param name="generar">Generacion y persistencia de un reporte.</param>
    /// <param name="listar">Historico de reportes generados.</param>
    /// <param name="obtener">Consulta de un reporte con su contenido.</param>
    /// <param name="exportar">Exportacion de un reporte a CSV descargable.</param>
    public ReportesController(
        GenerarReporteUseCase generar,
        ListarReportesUseCase listar,
        ObtenerReporteUseCase obtener,
        ExportarReporteUseCase exportar)
    {
        _generar = generar;
        _listar = listar;
        _obtener = obtener;
        _exportar = exportar;
    }

    /// <summary>Genera un reporte sobre las practicas que el filtro selecciona.</summary>
    /// <param name="request">Tipo declarado y criterios de seleccion.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El reporte generado, con su rastro y su contenido consolidado.</returns>
    /// <remarks>
    /// El tipo viaja en el cuerpo y no se deriva del numero de practicas que el
    /// filtro selecciona (O7). Un Individual que atrapa tres practicas responde
    /// 422 en lugar de convertirse en Grupal en silencio: el desajuste entre lo
    /// que se pidio y lo que el filtro devolvio es informacion, y derivar el tipo
    /// la habria borrado.
    ///
    /// <b>Responde 201 o 200, y la diferencia es observable.</b> El 201 lleva
    /// cabecera Location hacia el GET individual y significa que el reporte quedo
    /// persistido. El 200 significa que el filtro no selecciono ninguna practica:
    /// se devuelve el contenido vacio, con Id en cero y sin fecha, y <b>no se
    /// escribe nada</b> en reportes ni en reporte_practica (O8). No hay recurso al
    /// que apuntar, de modo que tampoco hay Location. Un filtro sin resultados no
    /// es un error del solicitante, es una respuesta.
    ///
    /// Un filtro que apunta fuera del alcance del solicitante cae en ese mismo
    /// 200 vacio y nunca en un 403 (O13): el Instructor que filtra por un aprendiz
    /// ajeno no llega a saber si ese aprendiz tiene practicas (RN-13).
    /// </remarks>
    /// <response code="201">Reporte generado y persistido.</response>
    /// <response code="200">El filtro no selecciono ninguna practica: contenido vacio, sin persistir nada.</response>
    /// <response code="400">El tipo no es Individual ni Grupal, o el rango de fechas del filtro esta invertido.</response>
    /// <response code="403">El rol autenticado es Aprendiz.</response>
    /// <response code="422">El estado o la modalidad del filtro traen un literal desconocido, o la composicion no es coherente con el tipo declarado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ReporteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReporteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Generar(GenerarReporteRequest request, CancellationToken ct)
    {
        var generado = await _generar.ExecuteAsync(request, ct);

        return generado.Id == 0
            ? Ok(generado)
            : CreatedAtAction(nameof(Obtener), new { id = generado.Id }, generado);
    }

    /// <summary>Lista el historico de reportes generados.</summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El rastro de cada reporte, del mas reciente al mas antiguo.</returns>
    /// <remarks>
    /// La ruta existia en la Matriz_de_Roles como la consulta que producia un
    /// reporte. Desde O1 producir es del POST y esta lista lo ya producido, que es
    /// lo que convierte a M7 en un modulo con memoria.
    ///
    /// El Administrador ve el historico completo y el Instructor solo los reportes
    /// que genero el mismo (RN-13). No hay parametros de filtro: el rastro de cada
    /// reporte ya lleva su tipo, sus criterios y su fecha, y filtrarlo en el
    /// cliente es mas util que decidir aqui por que campos se puede preguntar.
    ///
    /// Devuelve el rastro sin el contenido. Quien quiera las lineas y los totales
    /// de uno concreto pide el GET por identificador.
    /// </remarks>
    /// <response code="200">Listado de reportes, posiblemente vacio.</response>
    /// <response code="403">El rol autenticado es Aprendiz.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReporteResumenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await _listar.ExecuteAsync(ct));

    /// <summary>Obtiene un reporte por su identificador, con su contenido.</summary>
    /// <param name="id">Identificador del reporte.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El rastro del reporte y el contenido de las practicas que consolido.</returns>
    /// <remarks>
    /// El contenido se recompone en cada consulta sobre las practicas vinculadas,
    /// con los datos actuales (O14). Un reporte de hace un mes muestra las mismas
    /// practicas con los estados y los promedios de hoy: lo que se guardo fue la
    /// pregunta y su respuesta en identificadores, no una fotografia.
    ///
    /// <b>Un reporte ajeno responde 404 y no 403.</b> El Instructor que pide el
    /// reporte de otro recibe exactamente lo mismo que si el identificador no
    /// existiera, porque distinguir los dos casos le confirmaria que el recurso
    /// existe fuera de su alcance (RN-13). Es el mismo criterio con el que el POST
    /// devuelve vacio en lugar de prohibido.
    ///
    /// <b>El reporte propio tampoco se devuelve entero si el alcance cambio</b>
    /// (O20). Como el contenido es el de hoy, una practica que el Administrador
    /// reasigno desde que el reporte se genero sale de las lineas que ve el
    /// Instructor original, aunque siga en reporte_practica y aunque el
    /// Administrador la siga viendo. Si no queda ninguna, la respuesta es 200 con
    /// lineas vacias: el reporte sigue siendo suyo, su contenido ya no.
    /// </remarks>
    /// <response code="200">Reporte encontrado, con su contenido recompuesto y acotado al alcance de hoy.</response>
    /// <response code="403">El rol autenticado es Aprendiz.</response>
    /// <response code="404">El reporte no existe, o lo genero otro usuario y el solicitante es Instructor.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReporteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct) =>
        Ok(await _obtener.ExecuteAsync(id, ct));

    /// <summary>Exporta un reporte como archivo CSV descargable.</summary>
    /// <param name="id">Identificador del reporte.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El archivo CSV, con su nombre de descarga.</returns>
    /// <remarks>
    /// Es el unico endpoint del proyecto que no devuelve JSON, y el que cierra
    /// RF-08, CU-07 y HU-08b: los tres piden "exportacion en formato descargable"
    /// sin decir de que lado, y O21 la resuelve en el backend.
    ///
    /// <b>Devuelve exactamente lo que devuelve el GET anterior</b>, en otro
    /// formato. No es una promesa: ExportarReporteUseCase inyecta
    /// ObtenerReporteUseCase y formatea su salida (O23), de modo que la guarda del
    /// 404, el filtrado de alcance de O20 y la composicion del contenido son
    /// literalmente el mismo codigo. Los tres codigos de respuesta coinciden por la
    /// misma razon.
    ///
    /// El archivo lleva BOM UTF-8 y separador punto y coma (O22), para que Excel en
    /// Windows y en configuracion regional de Colombia lo abra en columnas y con
    /// las tildes correctas. Los decimales van con punto, que es lo unico de este
    /// formato que no se decide pensando en Excel.
    ///
    /// PDF y XLSX quedan fuera y difieridos a v2 (<b>FA-35</b>): los dos exigen una
    /// libreria de documentos, y sumar una dependencia a esta altura por un
    /// entregable secundario era mas riesgo que valor.
    /// </remarks>
    /// <response code="200">Archivo CSV del reporte, con su contenido acotado al alcance de hoy.</response>
    /// <response code="403">El rol autenticado es Aprendiz.</response>
    /// <response code="404">El reporte no existe, o lo genero otro usuario y el solicitante es Instructor.</response>
    [HttpGet("{id:int}/exportacion")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Exportar(int id, CancellationToken ct)
    {
        var archivo = await _exportar.ExecuteAsync(id, ct);

        return File(archivo.Contenido, archivo.TipoDeContenido, archivo.NombreDeArchivo);
    }
}
