namespace Practikap.Domain.Enums;

/// <summary>
/// Alcance funcional de un reporte generado.
/// Corresponde a la columna reportes.tipo del Script_DDL.sql.
/// </summary>
public enum TipoReporte
{
    /// <summary>Reporte sobre una sola practica. Literal en base de datos: "Individual".</summary>
    Individual = 1,

    /// <summary>Reporte consolidado sobre varias practicas. Literal en base de datos: "Grupal".</summary>
    Grupal = 2
}
