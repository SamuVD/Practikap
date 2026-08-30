using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Observaciones;
using Practikap.Application.UseCases.Observaciones;

namespace Practikap.API.Controllers;

/// <summary>
/// Anulacion de observaciones. Modulo M4, CU-04 (RF-04, RN-12).
/// </summary>
/// <remarks>
/// Controlador propio y no una accion mas de SeguimientosController (I12):
/// /api/observaciones es un recurso de primer nivel, y el Doc_Tecnico 5.3 pide
/// ruta literal por recurso. El alta si vive alla, porque su ruta es anidada
/// bajo el seguimiento al que la observacion pertenece.
///
/// Como el de seguimientos, no expone DELETE ni ningun verbo que edite el
/// contenido: el historial es inmutable por RN-12.
/// </remarks>
[ApiController]
[Route("api/observaciones")]
[Authorize(Roles = "Administrador")]
public sealed class ObservacionesController : ControllerBase
{
    private readonly AnularObservacionUseCase _anular;

    /// <summary>Crea el controlador.</summary>
    /// <param name="anular">Marca de anulacion.</param>
    public ObservacionesController(AnularObservacionUseCase anular) => _anular = anular;

    /// <summary>Marca una observacion como anulada.</summary>
    /// <param name="id">Observacion a anular.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>La observacion, con su marca y el identificador del anulador.</returns>
    /// <remarks>
    /// Es independiente de la anulacion del seguimiento al que la observacion
    /// pertenece: ninguna de las dos arrastra a la otra (I11). No lleva cuerpo,
    /// porque el actor sale del token.
    /// </remarks>
    /// <response code="200">Observacion anulada.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">La observacion no existe.</response>
    /// <response code="422">La observacion ya se encontraba anulada.</response>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(ObservacionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Anular(int id, CancellationToken ct) =>
        Ok(await _anular.ExecuteAsync(id, ct));
}
