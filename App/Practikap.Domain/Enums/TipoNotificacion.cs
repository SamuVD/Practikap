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
    Riesgo = 4
}
