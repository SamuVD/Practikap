using Practikap.Domain.Entities;

namespace Practikap.Application.Common;

/// <summary>
/// Punto unico de disparo del Motor de Reglas Dinamicas (RF-10, CU-02, RN-06,
/// N11). Los casos de uso que alteran un valor observable lo invocan; ninguno
/// carga reglas ni llama a <see cref="Domain.Rules.MotorDeReglas"/> por su cuenta.
/// </summary>
/// <remarks>
/// <para>
/// <b>Solo registra, nunca confirma.</b> No depende de IUnidadDeTrabajo y no tiene
/// forma de llamar a GuardarCambiosAsync. Quien confirma es el caso de uso
/// invocante, de modo que la calificacion, el cambio de estado de la practica y
/// las notificaciones caen en el mismo SaveChanges y en la misma transaccion
/// implicita del DbContext (ADR-02, N11). Es la misma garantia que sostiene
/// <see cref="IGeneradorDeNotificaciones"/>, y de ella sale la misma regla de
/// invocacion: la llamada va <b>antes</b> de GuardarCambiosAsync, no despues.
/// </para>
/// <para>
/// <b>El evaluador no es el Motor.</b> El Motor es
/// <see cref="Domain.Rules.MotorDeReglas"/>, que sigue siendo una clase estatica
/// del Dominio, sin EF, sin repositorios y sin async (ADR-04). Este servicio es lo
/// que lo rodea: carga las reglas activas ya ordenadas contra
/// <c>IReglaRepository.ListarActivasOrdenadasAsync</c>, mide el valor observado de
/// cada campo, le entrega las dos cosas al Motor y persiste la consecuencia de la
/// regla que gane.
/// </para>
/// <para>
/// <b>Un metodo por evento y no una firma generica</b>, con la misma forma que
/// tiene <see cref="IGeneradorDeNotificaciones"/>. Aqui el motivo es ademas
/// tecnico y no solo de legibilidad (N15): <c>PromedioVigenteAsync</c> traduce a
/// un AVG que MySQL evalua en el servidor, y EF Core no vuelca los cambios
/// pendientes antes de una consulta. Invocado antes de la confirmacion —que es
/// donde N11 lo obliga a estar— no veria la calificacion recien agregada, y la
/// primera calificacion de una practica se evaluaria contra el promedio anterior.
/// Cada metodo recibe por parametro el efecto que su caso de uso dejo pendiente, y
/// con el recompone el valor observado exacto sin necesitar una segunda
/// transaccion.
/// </para>
/// <para>
/// <b>Solo la direccion Instructor hacia Aprendiz dispara (N12).</b> Los dos casos
/// de uso con los que el Aprendiz evalua a su Instructor no invocan este contrato:
/// RN-09 mide el riesgo del aprendiz, y una nota baja puesta al instructor no lo
/// mide. Por eso las dos firmas nombran la calificacion del instructor y no
/// admiten la contraria.
/// </para>
/// <para>
/// El contrato es publico y su implementacion internal sealed, con el mismo
/// reparto que <see cref="IGeneradorDeNotificaciones"/> y que los repositorios: los
/// casos de uso que lo reciben son clases publicas, y un constructor publico no
/// admite un parametro de un tipo menos accesible.
/// </para>
/// </remarks>
public interface IEvaluadorDeReglas
{
    /// <summary>
    /// Evalua las reglas activas despues de que el Instructor registrara una
    /// calificacion sobre el Aprendiz, y aplica la consecuencia de la que gane
    /// (RN-06, RN-09).
    /// </summary>
    /// <param name="practica">
    /// Practica calificada, ya cargada y rastreada por el caso de uso. Se recibe la
    /// entidad y no su identificador para no repetir una consulta que la puerta de
    /// <c>AccesoALaPractica</c> ya hizo, y para que
    /// <see cref="Practica.MarcarEnRiesgo"/> actue sobre la instancia que EF Core
    /// esta siguiendo.
    /// </param>
    /// <param name="valorRegistrado">
    /// Valor de la calificacion que el caso de uso acaba de agregar y todavia no
    /// confirmo. Entra al promedio observado, que sin el quedaria una calificacion
    /// atrasado (N15).
    /// </param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task PorCalificacionRegistradaAsync(
        Practica practica, decimal valorRegistrado, CancellationToken ct);

    /// <summary>
    /// Evalua las reglas activas despues de que el Administrador anulara una
    /// calificacion del Instructor, y aplica la consecuencia de la que gane
    /// (RN-06, RN-09).
    /// </summary>
    /// <param name="practica">
    /// Practica a la que pertenecia la calificacion, ya cargada y rastreada.
    /// </param>
    /// <param name="calificacionAnuladaId">
    /// Identificador de la calificacion que el caso de uso acaba de marcar como
    /// anulada y todavia no confirmo. Sale del promedio observado, que sin esa
    /// exclusion seguiria contandola (N15, J5).
    /// </param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task PorCalificacionAnuladaAsync(
        Practica practica, int calificacionAnuladaId, CancellationToken ct);
}
