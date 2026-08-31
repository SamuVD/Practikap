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
///
/// <b>Donde deja de ser gemelo es en el Motor:</b> el de la direccion contraria lo
/// dispara y este no (N12). El promedio que esta anulacion mueve es el de las
/// evaluaciones que el aprendiz hizo de su instructor, y RN-09 no lo mira.
/// </remarks>
public sealed class AnularCalificacionAprendizUseCase
{
    private readonly ICalificacionAprendizRepository _calificacionRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IRegistradorDeAuditoria _auditor;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IMapper _mapeador;
    private readonly ILogger<AnularCalificacionAprendizUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="calificacionRepo">Acceso a las evaluaciones del aprendiz.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="auditor">Bitacora de acciones sensibles (P12, P13).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public AnularCalificacionAprendizUseCase(
        ICalificacionAprendizRepository calificacionRepo,
        IContextoUsuario contexto,
        IRegistradorDeAuditoria auditor,
        IUnidadDeTrabajo unidadDeTrabajo,
        IMapper mapeador,
        ILogger<AnularCalificacionAprendizUseCase> registro)
    {
        _calificacionRepo = calificacionRepo;
        _contexto = contexto;
        _auditor = auditor;
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

        // No hay llamada al Motor, a diferencia de la direccion contraria. La
        // Ronda 2 del 4.7 retiro de aqui el enganche que el 4.4 habia marcado
        // (N12). El de auditoria si esta en las dos: RN-12 no distingue
        // direcciones, y lo que se asienta es la anulacion, no su efecto sobre el
        // promedio.
        await _auditor.PorAnulacionAsync(
            EntidadAuditada.CalificacionesAprendiz, calificacion.Id, ct);

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Evaluacion del aprendiz {CalificacionId} anulada por el administrador {AdministradorId}.",
            calificacion.Id, _contexto.UsuarioId);

        return _mapeador.Map<CalificacionResponse>(calificacion);
    }
}
