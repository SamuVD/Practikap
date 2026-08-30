using AutoMapper;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Calificaciones;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Calificaciones;

/// <summary>
/// Marca como anulada una evaluacion emitida por el Aprendiz. Reservado al
/// Administrador y unica alteracion que RN-12 permite sobre el registro.
/// </summary>
/// <remarks>
/// Gemelo de <see cref="AnularCalificacionInstructorUseCase"/> sobre la otra
/// tabla, y separado de el por RN-10: anular una direccion no toca la contraria
/// ni consulta su repositorio.
///
/// Como alla, la guarda de la doble anulacion vive en CalificacionAprendiz.Anular
/// y no aqui, el registro anulado sigue saliendo en el listado con su marca, y lo
/// que cambia es el promedio vigente de esta direccion (J5).
/// </remarks>
public sealed class AnularCalificacionAprendizUseCase
{
    private readonly ICalificacionAprendizRepository _calificacionRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IMapper _mapeador;
    private readonly ILogger<AnularCalificacionAprendizUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="calificacionRepo">Acceso a las evaluaciones del aprendiz.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public AnularCalificacionAprendizUseCase(
        ICalificacionAprendizRepository calificacionRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IMapper mapeador,
        ILogger<AnularCalificacionAprendizUseCase> registro)
    {
        _calificacionRepo = calificacionRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Aplica la marca de anulacion.</summary>
    /// <param name="id">Evaluacion a anular.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La evaluacion, ya con su marca y el identificador del anulador.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no es Administrador (403).</exception>
    /// <exception cref="NoEncontradoException">Si la evaluacion no existe (404).</exception>
    /// <exception cref="ReglaDeDominioException">Si la evaluacion ya estaba anulada (422).</exception>
    public async Task<CalificacionResponse> ExecuteAsync(int id, CancellationToken ct)
    {
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException(
                "Solo el Administrador puede anular una calificacion.");

        var calificacion = await _calificacionRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("CalificacionAprendiz", id);

        calificacion.Anular(_contexto.UsuarioId);

        await _calificacionRepo.ActualizarAsync(calificacion, ct);
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        // Punto de enganche del Motor de Reglas (RN-06). El Motor llega en el
        // paso 4.7: aqui no se implementa ni se simula.

        _registro.LogInformation(
            "Evaluacion del aprendiz {CalificacionId} anulada por el administrador {AdministradorId}.",
            calificacion.Id, _contexto.UsuarioId);

        return _mapeador.Map<CalificacionResponse>(calificacion);
    }
}
