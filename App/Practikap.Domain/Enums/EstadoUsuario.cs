namespace Practikap.Domain.Enums;

/// <summary>
/// Estado de habilitacion de una cuenta de usuario.
/// Corresponde a la columna usuarios.estado del Script_DDL.sql.
/// </summary>
public enum EstadoUsuario
{
    /// <summary>La cuenta puede iniciar sesion. Literal en base de datos: "Activo".</summary>
    Activo = 1,

    /// <summary>La cuenta esta deshabilitada. Literal en base de datos: "Inactivo".</summary>
    Inactivo = 2
}
