using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Programas;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Programas;

/// <summary>
/// Lista los programas de formacion visibles para el solicitante (H19, H20).
/// </summary>
/// <remarks>
/// El Administrador ve el catalogo completo. El Instructor ve solo los programas
/// derivados de las practicas que tiene asignadas: el vinculo directo
/// instructor-programa no se implementa en v1 (H20, FA-27). El Aprendiz no
/// consulta este recurso.
/// </remarks>
public sealed class ListarProgramasUseCase
{
    private readonly IProgramaRepository _programaRepo;
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="programaRepo">Acceso a programas de formacion.</param>
    /// <param name="practicaRepo">Acceso a practicas, del que se derivan los programas del Instructor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarProgramasUseCase(
        IProgramaRepository programaRepo,
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        IMapper mapeador)
    {
        _programaRepo = programaRepo;
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve los programas del alcance del solicitante.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de programas.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante es un Aprendiz (403).</exception>
    public async Task<IReadOnlyList<ProgramaResponse>> ExecuteAsync(CancellationToken ct)
    {
        var programas = _contexto.Alcance switch
        {
            AlcanceConsulta.Global => await _programaRepo.ListarAsync(ct),
            AlcanceConsulta.Asignado => await DerivarDeLasPracticasAsync(ct),
            _ => throw new AutorizacionException(
                "El catalogo de programas no forma parte del alcance del Aprendiz.")
        };

        return _mapeador.Map<IReadOnlyList<ProgramaResponse>>(programas);
    }

    /// <summary>
    /// Deriva los programas del Instructor desde sus practicas asignadas. El grafo
    /// de PracticaRepository ya trae Ficha.Programa, asi que no hay consulta extra.
    /// </summary>
    private async Task<IReadOnlyList<Programa>> DerivarDeLasPracticasAsync(CancellationToken ct)
    {
        var practicas = await _practicaRepo.ListarPorInstructorAsync(_contexto.UsuarioId, ct);

        return practicas
            .Select(practica => practica.Ficha.Programa)
            .DistinctBy(programa => programa.Id)
            .OrderBy(programa => programa.Nombre)
            .ToList();
    }
}
