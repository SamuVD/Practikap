namespace Practikap.Application.DTOs.Reglas;

/// <summary>
/// Incorporacion o retiro de una regla de las evaluaciones del Motor. Es la
/// materializacion de RN-08 en el contrato HTTP.
/// </summary>
/// <param name="Activa">true incorpora la regla a las evaluaciones; false la retira.</param>
/// <remarks>
/// Las reglas nunca se eliminan: no hay DELETE (decision F3) y la clave foranea
/// fk_notificaciones_regla es ON DELETE RESTRICT, de modo que una regla que ya
/// disparo alertas no podria borrarse aunque el endpoint existiera. Desactivarla
/// es la baja, y conserva la traza de las notificaciones que origino (RN-09).
///
/// Mismo reparto que CambiarEstadoRequest en M1: un unico bool, sin validador,
/// porque el enlace de modelo de ASP.NET Core ya rechaza el cuerpo que no lo
/// traiga.
/// </remarks>
public sealed record CambiarActivaReglaRequest(bool Activa);
