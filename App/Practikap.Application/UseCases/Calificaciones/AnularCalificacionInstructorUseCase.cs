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
/// direccion, que deja de contarlo (J5).
/// </remarks>
public sealed class AnularCalificacionInstructorUseCase
{
    private readonly ICalificacionInstructorRepository _calificacionRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IMapper _mapeador;
    private readonly ILogger<AnularCalificacionInstructorUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="calificacionRepo">Acceso a las calificaciones del instructor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public AnularCalificacionInstructorUseCase(
        ICalificacionInstructorRepository calificacionRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IMapper mapeador,
        ILogger<AnularCalificacionInstructorUseCase> registro)
    {
        _calificacionRepo = calificacionRepo;
        _contexto = contexto;
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
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        // Punto de enganche del Motor de Reglas (RN-06). La Matriz_de_Roles
        // describe esta accion como "anula calificacion (recalcula motor)":
        // quitar una calificacion del promedio puede cruzar el umbral de RN-09 en
        // cualquiera de los dos sentidos. El Motor llega en el paso 4.7: aqui no
        // se implementa ni se simula.

        _registro.LogInformation(
            "Calificacion del instructor {CalificacionId} anulada por el administrador {AdministradorId}.",
            calificacion.Id, _contexto.UsuarioId);

        return _mapeador.Map<CalificacionResponse>(calificacion);
    }
}
