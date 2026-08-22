namespace Practikap.Domain.Enums;

/// <summary>
/// Causa por la que un token JWT quedo registrado como revocado (RN-03).
/// Corresponde a la columna tokens_revocados.motivo del Script_DDL.sql.
/// </summary>
public enum MotivoRevocacion
{
    /// <summary>Cierre de sesion explicito. Literal en base de datos: "Logout".</summary>
    Logout = 1,

    /// <summary>Cambio de credenciales. Literal en base de datos: "Cambio de contrasena" (con tilde en la enie).</summary>
    CambioContrasena = 2
}
