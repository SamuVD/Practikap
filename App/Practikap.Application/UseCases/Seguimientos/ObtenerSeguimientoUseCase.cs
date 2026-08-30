using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Seguimientos;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Seguimientos;

/// <summary>
/// Consulta un seguimiento por su identificador, con los tres alcances de RN-13
/// (I3).
/// </summary>
/// <remarks>
/// El Administrador ve cualquiera, el Instructor solo los de las practicas donde
/// figura como responsable y el Aprendiz solo los de la suya. El seguimiento
/// ajeno responde 403 y no 404: el recurso existe, lo que falta es el permiso.
/// Mismo criterio que ObtenerPracticaUseCase en M3.
///
/// Devuelve tambien los anulados, con su marca (I4).
/// </remarks>
public sealed class ObtenerSeguimientoUseCase
{
    private readonly ISeguimientoRepository _seguimientoRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="seguimientoRepo">Acceso a seguimientos.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ObtenerSeguimientoUseCase(
        ISeguimientoRepository seguimientoRepo, IContextoUsuario contexto, IMapper mapeador)
    {
        _seguimientoRepo = seguimientoRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve el seguimiento si el solicitante puede verlo.</summary>
    /// <param name="id">Identificador del seguimiento.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El seguimiento, con sus observaciones anidadas.</returns>
    /// <exception cref="AutorizacionException">Si queda fuera del alcance del solicitante (403).</exception>
    /// <exception cref="NoEncontradoException">Si el seguimiento no existe (404).</exception>
    public async Task<SeguimientoResponse> ExecuteAsync(int id, CancellationToken ct)
    {
        // El grafo trae la practica justamente para esto: el alcance se resuelve
        // sin una segunda consulta.
        var seguimiento = await _seguimientoRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Seguimiento", id);

        if (!AccesoALaPractica.EsVisible(seguimiento.Practica, _contexto))
            throw new AutorizacionException(
                "Solo puede consultar los seguimientos de su alcance.");

        return _mapeador.Map<SeguimientoResponse>(seguimiento);
    }
}
