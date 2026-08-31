using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Reglas;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Reglas;

/// <summary>
/// Consulta una regla por su identificador. Reservado al Administrador (RF-10,
/// CU-02).
/// </summary>
/// <remarks>
/// Existe por dos razones. Es la direccion a la que apunta la cabecera Location del
/// alta, cosa que el POST de notificaciones del paso 4.6 no pudo hacer por no tener
/// GET individual; y es lo que el panel consulta antes de editar, para no reenviar
/// en el PUT valores que no vio.
///
/// El 403 por alcance se comprueba antes de cargar, al reves que en
/// ObtenerPracticaUseCase: alli la respuesta dependia de quien fuera el instructor
/// o el aprendiz de esa practica en concreto, y hacia falta tenerla delante. Aqui
/// la puerta es solo el rol, y una regla ajena no existe.
/// </remarks>
public sealed class ObtenerReglaUseCase
{
    private readonly IReglaRepository _reglaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="reglaRepo">Acceso a las reglas del Motor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ObtenerReglaUseCase(
        IReglaRepository reglaRepo, IContextoUsuario contexto, IMapper mapeador)
    {
        _reglaRepo = reglaRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve la regla solicitada.</summary>
    /// <param name="id">Identificador de la regla.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La regla solicitada.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    /// <exception cref="NoEncontradoException">Si la regla no existe (404).</exception>
    public async Task<ReglaResponse> ExecuteAsync(int id, CancellationToken ct)
    {
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException("Solo el Administrador puede consultar el Motor de Reglas.");

        var regla = await _reglaRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Regla", id);

        return _mapeador.Map<ReglaResponse>(regla);
    }
}
