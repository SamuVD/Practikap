using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Calificaciones;
using Practikap.Application.UseCases.Seguimientos;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Calificaciones;

/// <summary>
/// Devuelve las calificaciones de una practica en sus dos direcciones, cada una
/// con su promedio vigente y con los tres alcances de RN-13 (RF-06, J2).
/// </summary>
/// <remarks>
/// Es el unico caso de uso de M5 que toca los dos repositorios, y no contradice
/// RN-10: no cruza las direcciones ni hace que una condicione a la otra, solo
/// las presenta juntas. Cada una se consulta por separado y se devuelve por
/// separado.
///
/// La practica se carga primero, y de proposito: su identificador viaja en la
/// consulta, de modo que una practica inexistente es un 404 legitimo, y sin
/// cargarla no habria como distinguir esa situacion de una practica todavia sin
/// calificar. El orden es 404 si no existe, 403 si esta fuera de alcance, y 200
/// con las listas —posiblemente vacias— en cualquier otro caso.
///
/// Los anulados entran en las listas con su marca, con el mismo criterio que I4
/// fijo en M4, y quedan fuera de los promedios (J5).
///
/// Son cuatro consultas y no dos. Los promedios podrian calcularse en memoria
/// sobre las listas ya traidas, pero eso duplicaria la definicion de promedio
/// vigente en un segundo lugar. PromedioVigenteAsync es el metodo que el Motor de
/// Reglas va a consultar en el paso 4.7 para el umbral de RN-09, y conviene que
/// haya uno solo.
/// </remarks>
public sealed class ListarCalificacionesDePracticaUseCase
{
    private readonly ICalificacionInstructorRepository _instructorRepo;
    private readonly ICalificacionAprendizRepository _aprendizRepo;
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="instructorRepo">Acceso a las calificaciones del instructor.</param>
    /// <param name="aprendizRepo">Acceso a las evaluaciones del aprendiz.</param>
    /// <param name="practicaRepo">Acceso a practicas, para resolver existencia y alcance.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarCalificacionesDePracticaUseCase(
        ICalificacionInstructorRepository instructorRepo,
        ICalificacionAprendizRepository aprendizRepo,
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        IMapper mapeador)
    {
        _instructorRepo = instructorRepo;
        _aprendizRepo = aprendizRepo;
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve las calificaciones si el solicitante puede ver la practica.</summary>
    /// <param name="practicaId">Practica cuyas calificaciones se consultan.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Las dos listas y los dos promedios vigentes de la practica.</returns>
    /// <exception cref="AutorizacionException">Si la practica queda fuera del alcance del solicitante (403).</exception>
    /// <exception cref="NoEncontradoException">Si la practica no existe (404).</exception>
    public async Task<CalificacionesDePracticaResponse> ExecuteAsync(
        int practicaId, CancellationToken ct)
    {
        var practica = await _practicaRepo.ObtenerPorIdAsync(practicaId, ct)
            ?? throw new NoEncontradoException("Practica", practicaId);

        // El mismo switch de RN-13 que resuelven los dos GET de M4 y los de M3.
        if (!AccesoALaPractica.EsVisible(practica, _contexto))
            throw new AutorizacionException(
                "Solo puede consultar las calificaciones de las practicas de su alcance.");

        var delInstructor = await _instructorRepo.ListarPorPracticaAsync(practicaId, ct);
        var delAprendiz = await _aprendizRepo.ListarPorPracticaAsync(practicaId, ct);

        var promedioInstructor = await _instructorRepo.PromedioVigenteAsync(practicaId, ct);
        var promedioAprendiz = await _aprendizRepo.PromedioVigenteAsync(practicaId, ct);

        return new CalificacionesDePracticaResponse(
            practicaId,
            promedioInstructor,
            promedioAprendiz,
            _mapeador.Map<IReadOnlyList<CalificacionResponse>>(delInstructor),
            _mapeador.Map<IReadOnlyList<CalificacionResponse>>(delAprendiz));
    }
}
