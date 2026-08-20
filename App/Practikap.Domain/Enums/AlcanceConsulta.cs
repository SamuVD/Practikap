namespace Practikap.Domain.Enums;

/// <summary>
/// Amplitud de datos que el solicitante tiene derecho a consultar, derivada de
/// su rol autenticado. Materializa RN-13 (aislamiento de datos por rol) en la
/// firma de las consultas de dominio, de modo que el alcance sea un parametro
/// explicito y no una omision silenciosa.
/// </summary>
/// <remarks>
/// No corresponde a ninguna columna del Script_DDL.sql: es un tipo de dominio.
/// Quien decide el valor es el caso de uso, a partir de IContextoUsuario (ADR-03).
/// </remarks>
public enum AlcanceConsulta
{
    /// <summary>Solo los registros propios del solicitante. Corresponde al rol Aprendiz.</summary>
    Propio = 1,

    /// <summary>Solo los registros de los aprendices asignados. Corresponde al rol Instructor.</summary>
    Asignado = 2,

    /// <summary>Todos los registros del sistema. Corresponde al rol Administrador.</summary>
    Global = 3
}
