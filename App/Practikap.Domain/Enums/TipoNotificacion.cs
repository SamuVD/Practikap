namespace Practikap.Domain.Enums;

/// <summary>
/// Origen funcional de una notificacion dirigida a un usuario.
/// Corresponde a la columna notificaciones.tipo del Script_DDL.sql.
/// </summary>
public enum TipoNotificacion
{
    /// <summary>Literal en base de datos: "Calificacion".</summary>
    Calificacion = 1,

    /// <summary>Literal en base de datos: "Mensaje".</summary>
    Mensaje = 2,

    /// <summary>Literal en base de datos: "Observacion".</summary>
    Observacion = 3,

    /// <summary>Disparada por el Motor de Reglas (RN-09). Literal en base de datos: "Riesgo".</summary>
    Riesgo = 4,

    /// <summary>
    /// Emitida a mano por el Administrador desde POST /api/notificaciones (L1,
    /// L2). Es el unico miembro que no nace de un evento del sistema. Literal en
    /// base de datos: "Administrativa".
    /// </summary>
    /// <remarks>
    /// Se anexa al final y no se intercala por orden alfabetico: MySQL conserva
    /// los ordinales de las filas ya escritas cuando el MODIFY COLUMN solo agrega
    /// valores despues del ultimo.
    /// </remarks>
    Administrativa = 5
}
