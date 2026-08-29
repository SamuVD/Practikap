using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Practicas;
using Practikap.Application.Validators.Practicas;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Practicas;

/// <summary>
/// Lista las practicas visibles para el solicitante, con filtros opcionales
/// (RF-03, H18, H19).
/// </summary>
/// <remarks>
/// El alcance de RN-13 se resuelve primero, eligiendo el metodo de repositorio,
/// y los filtros se aplican despues, sobre la coleccion ya restringida. Ese orden
/// es lo que hace que un filtro fuera del alcance devuelva una lista vacia con
/// 200 en lugar de 403: el solicitante no llega a saber si el recurso existe
/// fuera de su alcance, que es justo lo que RN-13 persigue.
///
/// Los filtros viven aqui y no en el repositorio (H27): IPracticaRepository no
/// gana parametros de filtro.
/// </remarks>
public sealed class ListarPracticasUseCase
{
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="practicaRepo">Acceso a practicas.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarPracticasUseCase(
        IPracticaRepository practicaRepo, IContextoUsuario contexto, IMapper mapeador)
    {
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve las practicas del alcance del solicitante que pasen los filtros.</summary>
    /// <param name="programaId">Programa por el que filtrar. Null no filtra.</param>
    /// <param name="fichaId">Ficha por la que filtrar. Null no filtra.</param>
    /// <param name="estado">Estado por el que filtrar, como texto. Null no filtra.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de practicas.</returns>
    /// <exception cref="AutorizacionException">Si el rol del token no es uno de los tres conocidos (403).</exception>
    /// <exception cref="ReglaDeDominioException">Si el estado indicado no es uno de los cuatro del ciclo de vida (422).</exception>
    public async Task<IReadOnlyList<PracticaResponse>> ExecuteAsync(
        int? programaId, int? fichaId, string? estado, CancellationToken ct)
    {
        var practicas = _contexto.Alcance switch
        {
            AlcanceConsulta.Global => await _practicaRepo.ListarTodasAsync(ct),
            AlcanceConsulta.Asignado =>
                await _practicaRepo.ListarPorInstructorAsync(_contexto.UsuarioId, ct),
            AlcanceConsulta.Propio =>
                await _practicaRepo.ListarPorAprendizAsync(_contexto.UsuarioId, ct),
            _ => throw new AutorizacionException(
                "El rol del token no corresponde a ninguno de los tres roles del sistema.")
        };

        IEnumerable<Practica> filtradas = practicas;

        // El programa no es columna de practicas: se deriva de la ficha. El grafo
        // ya lo trae cargado, asi que el filtro no cuesta una consulta mas.
        if (programaId is not null)
            filtradas = filtradas.Where(practica => practica.Ficha.ProgramaId == programaId.Value);

        if (fichaId is not null)
            filtradas = filtradas.Where(practica => practica.FichaId == fichaId.Value);

        if (!string.IsNullOrWhiteSpace(estado))
        {
            // Un literal desconocido no es un filtro fuera de alcance sino una
            // solicitud que no se puede procesar: 422, no lista vacia. Se compara
            // igual que en el validador, contra los nombres del enumerado (H31).
            if (!Enum.GetNames<EstadoPractica>().Contains(estado, StringComparer.Ordinal))
                throw new ReglaDeDominioException(
                    $"El estado debe ser uno de estos cuatro: {ReglasDeEnumerado.EstadosAdmitidos}.",
                    "RN-05");

            var estadoFiltro = Enum.Parse<EstadoPractica>(estado);
            filtradas = filtradas.Where(practica => practica.Estado == estadoFiltro);
        }

        return _mapeador.Map<IReadOnlyList<PracticaResponse>>(filtradas.ToList());
    }
}
