using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Mensajes;
using Practikap.Application.UseCases.Seguimientos;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Mensajes;

/// <summary>
/// Devuelve el hilo de mensajes de una practica, con los tres alcances de RN-13
/// (RF-07, K1, K4).
/// </summary>
/// <remarks>
/// La practica se carga primero, y de proposito: su identificador viaja en la
/// ruta, de modo que una practica inexistente es un 404 legitimo, y sin cargarla
/// no habria como distinguir esa situacion de una practica en la que todavia
/// nadie escribio. El orden es 404 si no existe, 403 si esta fuera de alcance, y
/// 200 con la lista —posiblemente vacia— en cualquier otro caso.
///
/// No hay guarda de estado, a diferencia del envio: leer se permite sobre
/// cualquiera de los cuatro estados (K3). Una practica Finalizada conserva su
/// conversacion y sigue siendo consultable; lo que ya no admite es un mensaje
/// nuevo.
///
/// El Administrador entra por AlcanceConsulta.Global, que es K4: lee cualquier
/// hilo con alcance de supervision, sin poder escribir en ninguno.
///
/// Devuelve una lista plana y no un objeto envolvente. M5 necesito uno porque
/// tenia dos direcciones en dos tablas; aqui hay un solo hilo en una sola tabla,
/// asi que la forma es la de ListarSeguimientosDePracticaUseCase.
/// </remarks>
public sealed class ListarMensajesDePracticaUseCase
{
    private readonly IMensajeRepository _mensajeRepo;
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="mensajeRepo">Acceso a los mensajes.</param>
    /// <param name="practicaRepo">Acceso a practicas, para resolver existencia y alcance.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarMensajesDePracticaUseCase(
        IMensajeRepository mensajeRepo,
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        IMapper mapeador)
    {
        _mensajeRepo = mensajeRepo;
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve el hilo si el solicitante puede ver la practica.</summary>
    /// <param name="practicaId">Practica cuyos mensajes se consultan.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Los mensajes de la practica, del mas antiguo al mas reciente.</returns>
    /// <exception cref="AutorizacionException">Si la practica queda fuera del alcance del solicitante (403).</exception>
    /// <exception cref="NoEncontradoException">Si la practica no existe (404).</exception>
    public async Task<IReadOnlyList<MensajeResponse>> ExecuteAsync(
        int practicaId, CancellationToken ct)
    {
        var practica = await _practicaRepo.ObtenerPorIdAsync(practicaId, ct)
            ?? throw new NoEncontradoException("Practica", practicaId);

        // El mismo switch de RN-13 que resuelven los dos GET de M4, el de M5 y
        // los de M3.
        if (!AccesoALaPractica.EsVisible(practica, _contexto))
            throw new AutorizacionException(
                "Solo puede consultar los mensajes de las practicas de su alcance.");

        var mensajes = await _mensajeRepo.ListarPorPracticaAsync(practicaId, ct);

        return _mapeador.Map<IReadOnlyList<MensajeResponse>>(mensajes);
    }
}
