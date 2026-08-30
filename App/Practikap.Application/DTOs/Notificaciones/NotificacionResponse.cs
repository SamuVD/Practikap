namespace Practikap.Application.DTOs.Notificaciones;

/// <summary>Representacion de salida de una notificacion interna.</summary>
/// <remarks>
/// UsuarioId es un identificador desnudo, sin nombre aplanado al lado.
/// NotificacionConfiguration mapea la clave foranea del destinatario con
/// HasOne&lt;Usuario&gt;().WithMany() y sin propiedad de navegacion, de modo que
/// ningun Usuario entra en el grafo y la fuga de ContrasenaHash que H32 vigila no
/// tiene por donde ocurrir (RNF-05).
///
/// ReglaId viaja como identificador y no como la regla entera, con el mismo
/// criterio: Notificacion.Regla si es navegacion, pero ninguna consulta del
/// repositorio la carga. Es nulo en todas las notificaciones que emite este paso
/// y solo lo poblara el Motor en el 4.7, junto al tipo Riesgo (RN-09, L7).
///
/// Tipo viaja como texto y no como numero (H31). Es el mismo literal que guarda
/// la columna ENUM.
///
/// Leida viaja siempre, tambien recien creada la notificacion, donde vale false.
/// Es lo que hace observable desde fuera el efecto del PATCH de L4.
/// </remarks>
/// <param name="Id">Identificador de la notificacion.</param>
/// <param name="UsuarioId">Usuario destinatario.</param>
/// <param name="ReglaId">Regla que la disparo, o null si no la origino el Motor.</param>
/// <param name="Tipo">Origen funcional, como texto.</param>
/// <param name="Contenido">Texto mostrado al destinatario.</param>
/// <param name="Leida">Marca de lectura, que solo el destinatario puede poner (L4).</param>
/// <param name="FechaGeneracion">Momento de la generacion, determinado por el servidor.</param>
public sealed record NotificacionResponse
(
    int Id,
    int UsuarioId,
    int? ReglaId,
    string Tipo,
    string Contenido,
    bool Leida,
    DateTime FechaGeneracion
);
