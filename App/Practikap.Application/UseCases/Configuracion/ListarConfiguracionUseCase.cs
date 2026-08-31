using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Configuracion;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Configuracion;

/// <summary>
/// Lista las entradas de configuracion del sistema. Reservado al Administrador
/// (RF-09, CU-08, RN-08).
/// </summary>
/// <remarks>
/// Devuelve las entradas <b>persistidas</b>, no el catalogo de P8. Una clave que
/// nunca se establecio no tiene fila y no aparece aqui: lo que se lista es lo que el
/// sistema tiene configurado, no lo que podria configurarse. Quien quiera saber que
/// claves admite el sistema lo descubre por el mensaje del 422 que devuelve el PUT,
/// que las enumera.
///
/// Sin alcance por rol, y no porque falte: todo M8 es del Administrador (P3), con un
/// unico alcance vivo, Global. No hay tres alcances de RN-13 que repartir aqui, hay
/// uno solo, y por eso la comprobacion es la misma que en los otros tres casos de uso
/// del modulo. Es la misma situacion de M2, y esta escrito con sus mismas palabras.
///
/// Sin parametros de filtro: el catalogo de P8 tiene dos claves y crecera en
/// unidades, no en miles.
/// </remarks>
public sealed class ListarConfiguracionUseCase
{
    private readonly IConfiguracionRepository _configuracionRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="configuracionRepo">Acceso al almacen clave/valor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarConfiguracionUseCase(
        IConfiguracionRepository configuracionRepo,
        IContextoUsuario contexto,
        IMapper mapeador)
    {
        _configuracionRepo = configuracionRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve las entradas de configuracion persistidas.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Las entradas, en orden alfabetico de clave.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    public async Task<IReadOnlyList<ConfiguracionResponse>> ExecuteAsync(CancellationToken ct)
    {
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException(
                "Solo el Administrador puede consultar la configuracion del sistema.");

        var entradas = await _configuracionRepo.ListarAsync(ct);

        return _mapeador.Map<IReadOnlyList<ConfiguracionResponse>>(entradas);
    }
}
