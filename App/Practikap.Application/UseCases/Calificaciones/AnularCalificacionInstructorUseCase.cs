using AutoMapper;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Calificaciones;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Calificaciones;

/// <summary>
/// Marca como anulada una calificacion emitida por el Instructor. Reservado al
/// Administrador y unica alteracion que RN-12 permite sobre el registro.
/// </summary>
/// <remarks>
/// Materializa J7 en su forma mas visible: el repositorio no sabe anular. Carga
/// la entidad rastreada, el Dominio aplica la marca y la unidad de trabajo
/// confirma. Que la doble anulacion devuelva 422 no es una comprobacion de este
/// caso de uso sino la guarda de CalificacionInstructor.Anular, que es donde la
/// regla pertenece.
///
/// La anulacion no se propaga a la direccion contraria, por el mismo motivo que
/// I11 dio en M4 y que RN-10 refuerza aqui: son registros independientes, y
/// arrastrar uno con el otro atribuiria al Administrador anulaciones que no
/// ejecuto.
///
/// El registro anulado sigue apareciendo en el listado con su marca y con el
/// identificador de quien la puso. Lo que si cambia es el promedio vigente de su
/// direccion, que deja de contarlo (J5). De ahi que sea uno de los dos
/// disparadores del Motor (N12): quitar una calificacion del promedio puede
/// cruzar el umbral de RN-09.
/// </remarks>
public sealed class AnularCalificacionInstructorUseCase
{
    private readonly ICalificacionInstructorRepository _calificacionRepo;
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IEvaluadorDeReglas _evaluador;
    private readonly IRegistradorDeAuditoria _auditor;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IMapper _mapeador;
    private readonly ILogger<AnularCalificacionInstructorUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="calificacionRepo">Acceso a las calificaciones del instructor.</param>
    /// <param name="practicaRepo">Acceso a practicas, para entregarle al Motor la que se recalcula.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="evaluador">Disparo del Motor de Reglas (RN-06, N11).</param>
    /// <param name="auditor">Bitacora de acciones sensibles (P12, P13).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public AnularCalificacionInstructorUseCase(
        ICalificacionInstructorRepository calificacionRepo,
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        IEvaluadorDeReglas evaluador,
        IRegistradorDeAuditoria auditor,
        IUnidadDeTrabajo unidadDeTrabajo,
        IMapper mapeador,
        ILogger<AnularCalificacionInstructorUseCase> registro)
    {
        _calificacionRepo = calificacionRepo;
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _evaluador = evaluador;
        _auditor = auditor;
        _unidadDeTrabajo = unidadDeTrabajo;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Aplica la marca de anulacion.</summary>
    /// <param name="id">Calificacion a anular.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La calificacion, ya con su marca y el identificador del anulador.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no es Administrador (403).</exception>
    /// <exception cref="NoEncontradoException">Si la calificacion no existe (404).</exception>
    /// <exception cref="ReglaDeDominioException">Si la calificacion ya estaba anulada (422).</exception>
    public async Task<CalificacionResponse> ExecuteAsync(int id, CancellationToken ct)
    {
        // Segunda barrera de ADR-03. La primera es el atributo del controlador;
        // esta es la que hace la regla verificable sin servidor web (RNF-08).
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException(
                "Solo el Administrador puede anular una calificacion.");

        var calificacion = await _calificacionRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("CalificacionInstructor", id);

        calificacion.Anular(_contexto.UsuarioId);

        await _calificacionRepo.ActualizarAsync(calificacion, ct);

        // El enganche del Motor (RN-06). La Matriz_de_Roles describe esta accion
        // como "anula calificacion (recalcula motor)".
        //
        // La practica se carga aqui y no antes: a diferencia de los dos POST, este
        // caso de uso no pasa por AccesoALaPractica. Quien anula es el
        // Administrador, que no participa en la practica, y su barrera es la de
        // Alcance == Global de mas arriba. La guarda de estado la pone el propio
        // evaluador (N14), que no actua sobre una practica que no este En curso.
        //
        // Va antes de la confirmacion, con el mismo criterio que el POST: el
        // evaluador solo registra (N11, ADR-02). Y recibe el identificador de la
        // calificacion anulada porque su marca todavia no esta en la base y el
        // promedio que MySQL calcularia seguiria contandola (N15).
        var practica = await _practicaRepo.ObtenerPorIdAsync(calificacion.PracticaId, ct);
        if (practica is not null)
            await _evaluador.PorCalificacionAnuladaAsync(practica, calificacion.Id, ct);

        // RN-12. Es el unico caso de uso del proyecto con los dos enganches
        // transversales de escritura, y los dos siguen la misma regla: registran y
        // no confirman. La calificacion, el eventual cambio de estado que el Motor
        // aplique, su notificacion y este asiento caen todos en el SaveChanges de
        // abajo (P12, N11, ADR-02).
        await _auditor.PorAnulacionAsync(
            EntidadAuditada.CalificacionesInstructor, calificacion.Id, ct);

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Calificacion del instructor {CalificacionId} anulada por el administrador {AdministradorId}.",
            calificacion.Id, _contexto.UsuarioId);

        return _mapeador.Map<CalificacionResponse>(calificacion);
    }
}
