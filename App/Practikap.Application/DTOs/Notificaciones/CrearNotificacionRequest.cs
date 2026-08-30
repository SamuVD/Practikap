namespace Practikap.Application.DTOs.Notificaciones;

/// <summary>
/// Datos de entrada de la notificacion administrativa que emite el Administrador
/// (L1, L2).
/// </summary>
/// <remarks>
/// Dos campos y ninguno mas. No declara Tipo: el caso de uso lo fija en
/// Administrativa, que es el valor que L2 agrego al ENUM justo para este
/// endpoint. Dejarlo viajar habria permitido al Administrador emitir avisos con
/// tipo Mensaje, Observacion o Calificacion sin que el evento correspondiente
/// hubiera ocurrido.
///
/// Tampoco declara ReglaId. Solo lo puebla el Motor de Reglas, y solo junto al
/// tipo Riesgo, que llega en el paso 4.7 (RN-09, L7).
///
/// Ni Leida ni FechaGeneracion: la primera nace en false y solo su destinatario
/// la cambia, por otra via; la segunda la genera MySQL con DEFAULT
/// CURRENT_TIMESTAMP.
/// </remarks>
/// <param name="UsuarioId">Usuario destinatario de la notificacion.</param>
/// <param name="Contenido">Texto que se le mostrara.</param>
public sealed record CrearNotificacionRequest
(
    int UsuarioId,
    string Contenido
);
