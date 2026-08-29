using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Practicas;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Practicas;

/// <summary>
/// Consulta una practica por su identificador, con los tres alcances de RN-13
/// (H15).
/// </summary>
/// <remarks>
/// El Administrador ve cualquiera, el Instructor solo aquellas en las que figura
/// como responsable y el Aprendiz solo la suya (H18). La practica ajena responde
/// 403 y no 404: el recurso existe, lo que falta es el permiso.
/// </remarks>
public sealed class ObtenerPracticaUseCase
{
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="practicaRepo">Acceso a practicas.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ObtenerPracticaUseCase(
        IPracticaRepository practicaRepo, IContextoUsuario contexto, IMapper mapeador)
    {
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve la practica si el solicitante puede verla.</summary>
    /// <param name="id">Identificador de la practica.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La practica solicitada.</returns>
    /// <exception cref="AutorizacionException">Si la practica queda fuera del alcance del solicitante (403).</exception>
    /// <exception cref="NoEncontradoException">Si la practica no existe (404).</exception>
    public async Task<PracticaResponse> ExecuteAsync(int id, CancellationToken ct)
    {
        var practica = await _practicaRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Practica", id);

        var esVisible = _contexto.Alcance switch
        {
            AlcanceConsulta.Global => true,
            AlcanceConsulta.Asignado => practica.InstructorId == _contexto.UsuarioId,
            AlcanceConsulta.Propio => practica.AprendizId == _contexto.UsuarioId,
            _ => false
        };

        if (!esVisible)
            throw new AutorizacionException("Solo puede consultar las practicas de su alcance.");

        return _mapeador.Map<PracticaResponse>(practica);
    }
}
