namespace Practikap.Domain.Enums;

/// <summary>
/// Tipo de accion sensible registrada en la bitacora de auditoria.
/// Corresponde a la columna auditoria.accion del Script_DDL.sql.
/// </summary>
public enum AccionAuditoria
{
    /// <summary>Anulacion de un registro del historial (RN-12). Literal: "Anulacion".</summary>
    Anulacion = 1,

    /// <summary>Retroceso de estado de una practica (RN-05). Literal: "Retroceso_estado".</summary>
    RetrocesoEstado = 2,

    /// <summary>Cambio de rol de un usuario (RN-01). Literal: "Cambio_rol".</summary>
    CambioRol = 3,

    /// <summary>Reasignacion de instructor o aprendiz (RN-04). Literal: "Reasignacion".</summary>
    Reasignacion = 4,

    /// <summary>Alta, edicion o desactivacion de una regla (RN-08). Literal: "Configuracion_regla".</summary>
    ConfiguracionRegla = 5,

    /// <summary>Cualquier otra accion sensible. Literal: "Otro".</summary>
    Otro = 6
}
