using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Configuracion;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Configuracion;

/// <summary>
/// Obtiene una entrada de configuracion por su clave. Reservado al Administrador
/// (RF-09, CU-08).
/// </summary>
/// <remarks>
/// <b>Una clave del catalogo que todavia no se establecio responde 404, igual que
/// una clave inventada.</b> Los dos casos son el mismo hecho: no hay fila. Que la
/// clave sea legitima no la convierte en un recurso existente, y devolver un 200 con
/// el valor vacio obligaria al cliente a distinguir "sin configurar" de "configurado
/// en vacio", que el validador ya prohibe.
///
/// La guarda del catalogo de P8 no se aplica aqui, y no por olvido: es una guarda de
/// <b>escritura</b>. Rechazar con 422 la lectura de una clave fuera del catalogo
/// daria dos codigos distintos para la misma respuesta —no hay nada que devolver— y
/// filtraria cual de las claves ausentes es legitima.
///
/// Sin alcance por rol: todo M8 es del Administrador (P3), con un unico alcance
/// vivo, Global.
/// </remarks>
public sealed class ObtenerConfiguracionUseCase
{
    private readonly IConfiguracionRepository _configuracionRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="configuracionRepo">Acceso al almacen clave/valor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ObtenerConfiguracionUseCase(
        IConfiguracionRepository configuracionRepo,
        IContextoUsuario contexto,
        IMapper mapeador)
    {
        _configuracionRepo = configuracionRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve la entrada de configuracion de una clave.</summary>
    /// <param name="clave">Clave de configuracion, tal como viaja en la ruta.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La entrada, con su valor vigente y su responsable.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    /// <exception cref="NoEncontradoException">Si la clave no esta configurada (404).</exception>
    public async Task<ConfiguracionResponse> ExecuteAsync(string clave, CancellationToken ct)
    {
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException(
                "Solo el Administrador puede consultar la configuracion del sistema.");

        var entrada = await _configuracionRepo.ObtenerPorClaveAsync(clave, ct)
            ?? throw new NoEncontradoException("Configuracion", clave);

        return _mapeador.Map<ConfiguracionResponse>(entrada);
    }
}
