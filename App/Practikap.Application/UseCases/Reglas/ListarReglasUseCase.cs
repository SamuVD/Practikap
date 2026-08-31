using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Reglas;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Reglas;

/// <summary>
/// Lista las reglas configuradas en el Motor. Reservado al Administrador (RF-10,
/// CU-02).
/// </summary>
/// <remarks>
/// Devuelve <b>todas</b> las reglas, activas e inactivas, y no solo las que el
/// Motor evalua: es el panel de administracion, y una regla retirada tiene que
/// poder verse para poder volver a activarse (RN-08). La consulta que el Motor usa
/// es la otra, ListarActivasOrdenadasAsync, y no se expone por HTTP.
///
/// Sin alcance por rol, y no porque falte: la Matriz_de_Roles hoja 2 le da al
/// Administrador acceso Total sobre M2 y deja a Instructor y Aprendiz sin acceso
/// alguno. No hay tres alcances de RN-13 que repartir aqui, hay uno solo, y por eso
/// la comprobacion es la misma que en los otros cuatro casos de uso del modulo.
///
/// El orden es el mismo con el que el Motor las evaluaria —prioridad ascendente,
/// desempatada por identificador—, de modo que el panel las muestra en el orden en
/// que se aplicarian (RN-07).
/// </remarks>
public sealed class ListarReglasUseCase
{
    private readonly IReglaRepository _reglaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="reglaRepo">Acceso a las reglas del Motor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarReglasUseCase(
        IReglaRepository reglaRepo, IContextoUsuario contexto, IMapper mapeador)
    {
        _reglaRepo = reglaRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve las reglas configuradas.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Las reglas, en el orden en que el Motor las evaluaria.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    public async Task<IReadOnlyList<ReglaResponse>> ExecuteAsync(CancellationToken ct)
    {
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException("Solo el Administrador puede consultar el Motor de Reglas.");

        var reglas = await _reglaRepo.ListarAsync(ct);

        return _mapeador.Map<IReadOnlyList<ReglaResponse>>(reglas);
    }
}
