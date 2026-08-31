using Practikap.Domain.Entities;

namespace Practikap.Application.Common;

/// <summary>
/// Punto unico de emision de notificaciones internas (RF-07, L6). Los casos de
/// uso que producen un evento notificable lo invocan; ninguno construye una
/// <see cref="Notificacion"/> por su cuenta.
/// </summary>
/// <remarks>
/// <para>
/// <b>Solo registra, nunca confirma.</b> No depende de IUnidadDeTrabajo y no
/// tiene forma de llamar a GuardarCambiosAsync. Quien confirma es el caso de uso
/// invocante, de modo que el evento y su notificacion caen en el mismo
/// SaveChanges y, por tanto, en la misma transaccion implicita del DbContext
/// (ADR-02). Esa es la garantia entera de L6: si el evento falla al confirmarse,
/// no queda una notificacion huerfana anunciando algo que nunca ocurrio.
/// </para>
/// <para>
/// De ahi tambien la regla de invocacion: la llamada va <b>antes</b> de
/// GuardarCambiosAsync, no despues. Colocarla despues produciria dos
/// confirmaciones separadas y romperia la promesa sin que nada dejara de
/// compilar.
/// </para>
/// <para>
/// Un metodo por evento y no una firma generica con el tipo y el texto por
/// parametro. La redaccion vive en un solo archivo, el
/// <see cref="Domain.Enums.TipoNotificacion"/> lo elige quien sabe de
/// notificaciones y no cada modulo invocante, y en el punto de enganche de M4, M5
/// y 4.5 queda una sola linea legible.
/// </para>
/// <para>
/// Como los cuatro primeros metodos no reciben texto libre, el contenido que
/// generan es una plantilla fija con identificadores numericos y cabe por
/// construccion en el VARCHAR(255) de la columna. El unico que admite texto de
/// afuera es <see cref="AdministrativaAsync"/>, y su validador acota el largo en
/// esos mismos 255.
/// </para>
/// <para>
/// <b>El tipo Riesgo entra por su propio metodo</b>, que es el que la Ronda 2 del
/// paso 4.7 agrego y que L7 dejaba marcado. Nace de una regla y no de una accion
/// de usuario, y debe dejar poblado regla_id conforme a RN-09, para lo cual el
/// Dominio trae la fabrica Notificacion.DesdeRegla. De las cinco emisiones es la
/// unica que no pasa por el constructor plano de la entidad, porque es la unica
/// que tiene una regla que trazar. Su unico invocante es
/// <see cref="IEvaluadorDeReglas"/>: ningun caso de uso la emite por su cuenta.
/// </para>
/// <para>
/// El contrato es publico y su implementacion internal sealed, con el mismo
/// reparto que tienen los repositorios: los casos de uso que lo reciben son
/// clases publicas, y un constructor publico no admite un parametro de un tipo
/// menos accesible. Que la implementacion no salga del ensamblado es lo que
/// mantiene el punto de emision unico.
/// </para>
/// </remarks>
public interface IGeneradorDeNotificaciones
{
    /// <summary>
    /// Notifica al aprendiz que su instructor registro una observacion sobre el
    /// seguimiento de su practica (RF-07, CU-06, L5).
    /// </summary>
    /// <param name="destinatarioId">Aprendiz de la practica.</param>
    /// <param name="practicaId">Practica en la que se registro la observacion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task PorObservacionAsync(int destinatarioId, int practicaId, CancellationToken ct);

    /// <summary>
    /// Notifica a la contraparte que se registro una calificacion sobre la
    /// practica que comparten (RF-07, CU-06, L5).
    /// </summary>
    /// <param name="destinatarioId">
    /// Quien recibe la calificacion: el aprendiz cuando califica el instructor, y
    /// el instructor cuando lo evalua el aprendiz. Un solo metodo para las dos
    /// direcciones, porque el texto es el mismo y RN-10 solo exige que los
    /// registros sean independientes, no que lo sea el aviso.
    /// </param>
    /// <param name="practicaId">Practica sobre la que se califico.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task PorCalificacionAsync(int destinatarioId, int practicaId, CancellationToken ct);

    /// <summary>
    /// Notifica al destinatario que recibio un mensaje nuevo en la mensajeria
    /// interna (RF-07, CU-06, K7, L5).
    /// </summary>
    /// <param name="destinatarioId">Receptor del mensaje.</param>
    /// <param name="practicaId">Practica que enmarca la conversacion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task PorMensajeAsync(int destinatarioId, int practicaId, CancellationToken ct);

    /// <summary>
    /// Notifica al instructor que una regla activa del Motor coincidio sobre una
    /// de sus practicas (RF-07, CU-06, RN-09, L7).
    /// </summary>
    /// <param name="destinatarioId">Instructor responsable de la practica.</param>
    /// <param name="practicaId">Practica sobre la que la regla coincidio.</param>
    /// <param name="reglaId">
    /// Regla que produjo la alerta. Queda escrita en notificaciones.regla_id, que
    /// es lo que RN-09 exige para que la alerta sea trazable hasta la
    /// configuracion que la origino.
    /// </param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <remarks>
    /// El texto es neutro a proposito y no afirma que la practica haya quedado En
    /// riesgo: la misma notificacion sirve para la accion NotificarInstructor, que
    /// avisa sin cambiar el estado. Quien decide que efectos se aplican es el
    /// evaluador, y este metodo solo emite el aviso que le corresponda.
    /// </remarks>
    Task PorRiesgoAsync(int destinatarioId, int practicaId, int reglaId, CancellationToken ct);

    /// <summary>
    /// Emite la notificacion administrativa que el Administrador redacta a mano
    /// desde POST /api/notificaciones (L1, L2).
    /// </summary>
    /// <param name="destinatarioId">Usuario al que va dirigida.</param>
    /// <param name="contenido">Texto redactado por el Administrador.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>
    /// La notificacion registrada, sin confirmar. Es el unico de los cuatro
    /// metodos que devuelve la entidad, porque es el unico cuyo caso de uso la
    /// mapea a una respuesta; los otros tres son efectos laterales de un evento
    /// que ya tiene su propia salida.
    /// </returns>
    Task<Notificacion> AdministrativaAsync(int destinatarioId, string contenido, CancellationToken ct);
}
