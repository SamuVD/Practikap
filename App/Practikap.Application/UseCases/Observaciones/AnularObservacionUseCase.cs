using AutoMapper;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Observaciones;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Observaciones;

/// <summary>
/// Marca una observacion como anulada. Reservado al Administrador (I1, RN-12).
/// </summary>
/// <remarks>
/// Simetrico a AnularSeguimientoUseCase y sujeto al mismo criterio de I9: la
/// marca la aplica el Dominio, no el repositorio.
///
/// Se puede anular una observacion cuyo seguimiento sigue vigente y tambien una
/// cuyo seguimiento ya fue anulado: son marcas independientes (I11), y exigir un
/// orden entre ellas dejaria registros imposibles de corregir.
/// </remarks>
public sealed class AnularObservacionUseCase
{
    private readonly IObservacionRepository _observacionRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IMapper _mapeador;
    private readonly ILogger<AnularObservacionUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="observacionRepo">Acceso a observaciones.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public AnularObservacionUseCase(
        IObservacionRepository observacionRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IMapper mapeador,
        ILogger<AnularObservacionUseCase> registro)
    {
        _observacionRepo = observacionRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Aplica la marca de anulacion.</summary>
    /// <param name="id">Observacion a anular.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La observacion, ya con su marca y el identificador del anulador.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no es Administrador (403).</exception>
    /// <exception cref="NoEncontradoException">Si la observacion no existe (404).</exception>
    /// <exception cref="ReglaDeDominioException">Si la observacion ya estaba anulada (422).</exception>
    public async Task<ObservacionResponse> ExecuteAsync(int id, CancellationToken ct)
    {
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException(
                "Solo el Administrador puede anular una observacion.");

        var observacion = await _observacionRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Observacion", id);

        observacion.Anular(_contexto.UsuarioId);

        await _observacionRepo.ActualizarAsync(observacion, ct);
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Observacion {ObservacionId} anulada por el administrador {AdministradorId}.",
            observacion.Id, _contexto.UsuarioId);

        return _mapeador.Map<ObservacionResponse>(observacion);
    }
}
