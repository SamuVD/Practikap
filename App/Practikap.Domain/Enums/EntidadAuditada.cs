namespace Practikap.Domain.Enums;

/// <summary>
/// Enumeración cerrada de las entidades sobre las que la bitacora puede
/// registrar una accion. Materializa ADR-06: la referencia de auditoria es
/// polimorfica y no tiene clave foranea fisica, por lo que el conjunto de
/// destinos posibles se cierra aqui en lugar de dejarlo como texto libre.
/// Corresponde a la columna auditoria.entidad_afectada del Script_DDL.sql,
/// cuyos literales son los nombres de tabla en minuscula.
/// </summary>
public enum EntidadAuditada
{
    /// <summary>Literal en base de datos: "usuarios".</summary>
    Usuarios = 1,

    /// <summary>Literal en base de datos: "practicas".</summary>
    Practicas = 2,

    /// <summary>Literal en base de datos: "seguimientos".</summary>
    Seguimientos = 3,

    /// <summary>Literal en base de datos: "observaciones".</summary>
    Observaciones = 4,

    /// <summary>Literal en base de datos: "calificaciones_instructor".</summary>
    CalificacionesInstructor = 5,

    /// <summary>Literal en base de datos: "calificaciones_aprendiz".</summary>
    CalificacionesAprendiz = 6,

    /// <summary>Literal en base de datos: "reglas".</summary>
    Reglas = 7,

    /// <summary>Literal en base de datos: "configuracion".</summary>
    Configuracion = 8
}
