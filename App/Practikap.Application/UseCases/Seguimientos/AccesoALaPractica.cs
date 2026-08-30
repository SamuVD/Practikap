using Practikap.Application.Common;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Seguimientos;

/// <summary>
/// Las dos preguntas que M4 y M5 le hacen a una practica antes de tocarla: si el
/// solicitante puede escribir sobre ella (I2, I7, J4) y si puede verla (RN-13).
/// </summary>
/// <remarks>
/// Es la pieza compartida de los dos modulos, con la misma forma que
/// ParticipantesDePractica tiene en M3: clase estatica, sin estado ni
/// dependencias propias, que no entra en el contenedor y no contradice ADR-05.
///
/// No reutiliza ParticipantesDePractica porque aquella resuelve otro problema:
/// verifica un par instructor-aprendiz que llega en el cuerpo de la peticion. En
/// M4 y M5 nadie manda participantes. El emisor es el del token y la pareja ya
/// vive en la practica, de modo que lo que hay que comprobar no es quienes son
/// sino si el solicitante es uno de ellos.
///
/// Vive en el espacio de nombres de M4 porque alli nacio, y M5 la consume desde
/// el suyo: moverla habria obligado a tocar los cuatro casos de uso de un modulo
/// ya cerrado a cambio de nada verificable.
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

        VerificarEstadoAdmiteRegistro(practica, "RN-12");

        return practica;
    }

    /// <summary>
    /// La misma comprobacion que <see cref="VerificarEscrituraAsync"/>, pero
    /// contra el aprendiz de la practica. Es la puerta del POST con el que el
    /// Aprendiz evalua a su instructor (J2, direccion Aprendiz hacia Instructor).
    /// </summary>
    /// <param name="practicaRepo">Acceso a practicas, del modulo M3.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="practicaId">Practica sobre la que se pretende escribir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La practica, si las tres condiciones se cumplen.</returns>
    /// <exception cref="AutorizacionException">
    /// Si el solicitante no es el aprendiz de la practica (403).
    /// </exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si la practica no existe, o si su estado no admite calificaciones (422, J4).
    /// </exception>
    /// <remarks>
    /// Existe como metodo aparte y no como un parametro de la anterior para no
    /// alterar la firma que los tres casos de uso de M4 ya invocan. Lo unico que
    /// las dos comparten es la guarda de estado, que si esta factorizada.
    /// </remarks>
    public static async Task<Practica> VerificarEscrituraDelAprendizAsync(
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        int practicaId,
        CancellationToken ct)
    {
        // 422 y no 404, por el mismo motivo que en la version del instructor: el
        // identificador viaja en el cuerpo del POST y no en la ruta.
        var practica = await practicaRepo.ObtenerPorIdAsync(practicaId, ct)
            ?? throw new ReglaDeDominioException(
                $"La practica {practicaId} no existe.", "RN-13");

        if (practica.AprendizId != contexto.UsuarioId)
            throw new AutorizacionException(
                "Solo puede evaluar la practica en la que usted es el aprendiz.");

        VerificarEstadoAdmiteRegistro(practica, "RN-10");

        return practica;
    }

    /// <summary>
    /// Comprueba que el estado de la practica admita registros nuevos: solo
    /// En curso y En riesgo.
    /// </summary>
    /// <param name="practica">Practica ya cargada.</param>
    /// <param name="regla">
    /// Regla a la que se atribuye el 422. M4 lo imputa a RN-12, que es la que
    /// gobierna el historial de seguimiento; M5 a RN-10, que es la de la
    /// calificacion bidireccional.
    /// </param>
    /// <exception cref="ReglaDeDominioException">Si el estado no lo admite (422).</exception>
    /// <remarks>
    /// I2 y J4 dicen lo mismo con distinto nombre, y por eso comparten guarda.
    /// En riesgo entra junto a En curso: es un estado de alerta del Motor
    /// (RN-09), no una practica detenida, y es justo cuando el seguimiento y la
    /// calificacion mas importan. Pendiente y Finalizada quedan fuera.
    /// </remarks>
    private static void VerificarEstadoAdmiteRegistro(Practica practica, string regla)
    {
        if (practica.Estado is not (EstadoPractica.EnCurso or EstadoPractica.EnRiesgo))
            throw new ReglaDeDominioException(
                "Solo se puede registrar sobre practicas En curso o En riesgo.", regla);
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
