namespace Practikap.Application.DTOs.Usuarios;

/// <summary>
/// Habilitacion o deshabilitacion de una cuenta.
/// </summary>
/// <param name="Activo">true habilita la cuenta; false la deshabilita.</param>
/// <remarks>
/// Las cuentas nunca se eliminan: las claves foraneas usan ON DELETE RESTRICT y
/// el historial de practicas debe conservarse. Por eso no existe DELETE sobre
/// /api/usuarios (decision F3).
/// </remarks>
public sealed record CambiarEstadoRequest(bool Activo);