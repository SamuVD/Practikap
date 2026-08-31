using AutoMapper;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Seguimientos;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Seguimientos;

/// <summary>
/// Marca un seguimiento como anulado. Reservado al Administrador y unica
/// alteracion del historial que RN-12 permite.
/// </summary>
/// <remarks>
/// Materializa I9 en su forma mas visible: el repositorio no sabe anular. Carga
/// la entidad rastreada, el Dominio aplica la marca y la unidad de trabajo
/// confirma. Que la doble anulacion devuelva 422 no es una comprobacion de este
/// caso de uso sino la guarda de Seguimiento.Anular, que es donde la regla
/// pertenece.
///
/// No se propaga a las observaciones (I11). Anular el seguimiento y anular cada
/// observacion son actos distintos, y mezclarlos atribuiria al Administrador
/// anulaciones que no ejecuto, falseando la traza que RN-12 existe para
/// preservar. El historial devuelve el seguimiento anulado con sus observaciones
/// todavia vigentes, que es la situacion real.
/// </remarks>
public sealed class AnularSeguimientoUseCase
{
    private readonly ISeguimientoRepository _seguimientoRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IRegistradorDeAuditoria _auditor;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IMapper _mapeador;
    private readonly ILogger<AnularSeguimientoUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="seguimientoRepo">Acceso a seguimientos.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="auditor">Bitacora de acciones sensibles (P12, P13).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public AnularSeguimientoUseCase(
        ISeguimientoRepository seguimientoRepo,
        IContextoUsuario contexto,
        IRegistradorDeAuditoria auditor,
        IUnidadDeTrabajo unidadDeTrabajo,
        IMapper mapeador,
        ILogger<AnularSeguimientoUseCase> registro)
    {
        _seguimientoRepo = seguimientoRepo;
        _contexto = contexto;
        _auditor = auditor;
        _unidadDeTrabajo = unidadDeTrabajo;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Aplica la marca de anulacion.</summary>
    /// <param name="id">Seguimiento a anular.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El seguimiento, ya con su marca y el identificador del anulador.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no es Administrador (403).</exception>
    /// <exception cref="NoEncontradoException">Si el seguimiento no existe (404).</exception>
    /// <exception cref="ReglaDeDominioException">Si el seguimiento ya estaba anulado (422).</exception>
    public async Task<SeguimientoResponse> ExecuteAsync(int id, CancellationToken ct)
    {
        // Segunda barrera de ADR-03. La primera es el atributo del controlador;
        // esta es la que hace la regla verificable sin servidor web (RNF-08).
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException(
                "Solo el Administrador puede anular un seguimiento.");

        var seguimiento = await _seguimientoRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Seguimiento", id);

        seguimiento.Anular(_contexto.UsuarioId);

        await _seguimientoRepo.ActualizarAsync(seguimiento, ct);

        // RN-12. Una de las cuatro anulaciones, todas con el mismo metodo y la
        // entidad por parametro (P13).
        await _auditor.PorAnulacionAsync(EntidadAuditada.Seguimientos, seguimiento.Id, ct);

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Seguimiento {SeguimientoId} anulado por el administrador {AdministradorId}.",
            seguimiento.Id, _contexto.UsuarioId);

        return _mapeador.Map<SeguimientoResponse>(seguimiento);
    }
}
