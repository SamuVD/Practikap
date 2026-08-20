namespace Practikap.Domain.Enums;

/// <summary>
/// Estado del ciclo de vida de una practica productiva.
/// Corresponde a la columna practicas.estado del Script_DDL.sql.
/// La secuencia permitida y la reserva del retroceso al Administrador
/// las gobierna RN-05, implementada en <c>Practica.CambiarEstado</c>.
/// </summary>
public enum EstadoPractica
{
    /// <summary>Practica creada y aun no iniciada. Literal en base de datos: "Pendiente".</summary>
    Pendiente = 1,

    /// <summary>Practica en desarrollo. Literal en base de datos: "En curso".</summary>
    EnCurso = 2,

    /// <summary>Practica cerrada. Literal en base de datos: "Finalizada".</summary>
    Finalizada = 3,

    /// <summary>Marcada por el Motor de Reglas (RN-09). Literal en base de datos: "En riesgo".</summary>
    EnRiesgo = 4
}
