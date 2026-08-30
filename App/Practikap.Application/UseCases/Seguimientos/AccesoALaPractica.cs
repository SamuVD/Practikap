using Practikap.Application.Common;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Seguimientos;

/// <summary>
/// Las dos preguntas que M4 le hace a una practica antes de tocarla: si el
/// solicitante puede escribir sobre ella (I2 e I7) y si puede verla (RN-13).
/// </summary>
/// <remarks>
/// Es la pieza compartida del modulo, con la misma forma que
/// ParticipantesDePractica tiene en M3: clase estatica, sin estado ni
/// dependencias propias, que no entra en el contenedor y no contradice ADR-05.
///
/// No reutiliza ParticipantesDePractica porque aquella resuelve otro problema:
/// verifica un par instructor-aprendiz que llega en el cuerpo de la peticion. En
/// M4 nadie manda participantes. El instructor es el del token y la pareja ya
/// vive en la practica, de modo que lo que hay que comprobar no es quienes son
/// sino si el solicitante es uno de ellos.
/// </remarks>
internal static class AccesoALaPractica
{
    /// <summary>
    /// Comprueba que la practica exista, que el solicitante sea su instructor y
    /// que su estado admita registros nuevos. Devuelve la practica ya cargada.
    /// </summary>
    /// <param name="practicaRepo">Acceso a practicas, del modulo M3.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="practicaId">Practica sobre la que se pretende escribir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La practica, si las tres condiciones se cumplen.</returns>
    /// <exception cref="AutorizacionException">
    /// Si el solicitante no es el instructor responsable de la practica (403, I7).
    /// </exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si la practica no existe, o si su estado no admite seguimientos (422, I2).
    /// </exception>
    public static async Task<Practica> VerificarEscrituraAsync(
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        int practicaId,
        CancellationToken ct)
    {
        // 422 y no 404, con el mismo criterio que ParticipantesDePractica: el
        // identificador de la practica no viaja en la ruta en ninguno de los dos
        // POST. En el alta va en el cuerpo, y en la ruta anidada el id de ruta es
        // el del seguimiento, del que la practica se deriva. No es el recurso
        // pedido lo que falta, es la solicitud lo que no se puede procesar.
        var practica = await practicaRepo.ObtenerPorIdAsync(practicaId, ct)
            ?? throw new ReglaDeDominioException(
                $"La practica {practicaId} no existe.", "RN-13");

        // I7. La autoria se comprueba antes que el estado a proposito: RN-13 no
        // quiere que un solicitante ajeno llegue a enterarse de en que estado
        // esta una practica que no le corresponde.
        if (practica.InstructorId != contexto.UsuarioId)
            throw new AutorizacionException(
                "Solo puede registrar sobre las practicas en las que usted es el instructor.");

        // I2. En riesgo entra junto a En curso: es un estado de alerta del Motor
        // (RN-09), no una practica detenida, y es justo cuando el seguimiento mas
        // importa. Pendiente y Finalizada quedan fuera.
        if (practica.Estado is not (EstadoPractica.EnCurso or EstadoPractica.EnRiesgo))
            throw new ReglaDeDominioException(
                "Solo se puede registrar sobre practicas En curso o En riesgo.", "RN-12");

        return practica;
    }

    /// <summary>
    /// Indica si el solicitante puede ver los registros de una practica, segun
    /// los tres alcances de RN-13.
    /// </summary>
    /// <param name="practica">Practica a la que pertenecen los registros.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <returns>true si la practica cae dentro del alcance del solicitante.</returns>
    /// <remarks>
    /// Mismo switch que ObtenerPracticaUseCase y ListarPracticasUseCase aplican
    /// en M3 (H18). Vive aqui porque M4 lo necesita en dos casos de uso: la
    /// consulta de un seguimiento suelto y la del historial de una practica.
    ///
    /// Devuelve un booleano en lugar de lanzar para que cada caso de uso redacte
    /// su propio mensaje de 403.
    /// </remarks>
    public static bool EsVisible(Practica practica, IContextoUsuario contexto) =>
        contexto.Alcance switch
        {
            AlcanceConsulta.Global => true,
            AlcanceConsulta.Asignado => practica.InstructorId == contexto.UsuarioId,
            AlcanceConsulta.Propio => practica.AprendizId == contexto.UsuarioId,
            _ => false
        };
}
