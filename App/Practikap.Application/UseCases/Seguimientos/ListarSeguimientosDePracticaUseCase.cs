using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Seguimientos;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Seguimientos;

/// <summary>
/// Devuelve el historial de seguimientos de una practica, con las observaciones
/// de cada uno anidadas (RF-04, RF-05, I5).
/// </summary>
/// <remarks>
/// Es el endpoint que CU-04 nombra como historial inmutable, y el que alimenta
/// el panel del Aprendiz.
///
/// La practica se carga primero, y de proposito: aqui el identificador si viaja
/// en la ruta, de modo que una practica inexistente es un 404 legitimo, y sin
/// cargarla no habria como distinguir esa situacion de un historial todavia
/// vacio. El orden es 404 si no existe, 403 si esta fuera de alcance, y 200 con
/// la lista —posiblemente vacia— en cualquier otro caso.
///
/// Los anulados entran en la lista con su marca (I4).
/// </remarks>
public sealed class ListarSeguimientosDePracticaUseCase
{
    private readonly ISeguimientoRepository _seguimientoRepo;
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="seguimientoRepo">Acceso a seguimientos.</param>
    /// <param name="practicaRepo">Acceso a practicas, para resolver existencia y alcance.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarSeguimientosDePracticaUseCase(
        ISeguimientoRepository seguimientoRepo,
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        IMapper mapeador)
    {
        _seguimientoRepo = seguimientoRepo;
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve el historial si el solicitante puede ver la practica.</summary>
    /// <param name="practicaId">Practica cuyo historial se consulta.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Los seguimientos de la practica, del mas reciente al mas antiguo.</returns>
    /// <exception cref="AutorizacionException">Si la practica queda fuera del alcance del solicitante (403).</exception>
    /// <exception cref="NoEncontradoException">Si la practica no existe (404).</exception>
    public async Task<IReadOnlyList<SeguimientoResponse>> ExecuteAsync(
        int practicaId, CancellationToken ct)
    {
        var practica = await _practicaRepo.ObtenerPorIdAsync(practicaId, ct)
            ?? throw new NoEncontradoException("Practica", practicaId);

        if (!AccesoALaPractica.EsVisible(practica, _contexto))
            throw new AutorizacionException(
                "Solo puede consultar el historial de las practicas de su alcance.");

        var seguimientos = await _seguimientoRepo.ListarPorPracticaAsync(practicaId, ct);

        return _mapeador.Map<IReadOnlyList<SeguimientoResponse>>(seguimientos);
    }
}
