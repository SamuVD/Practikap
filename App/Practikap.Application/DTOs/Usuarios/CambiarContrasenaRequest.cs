namespace Practikap.Application.DTOs.Usuarios;

/// <summary>
/// Cambio de contrasena por el propio usuario (D7).
/// </summary>
/// <param name="ContrasenaActual">Contrasena vigente. Se exige para impedir el secuestro de una sesion abierta.</param>
/// <param name="ContrasenaNueva">Contrasena de reemplazo.</param>
public sealed record CambiarContrasenaRequest(string ContrasenaActual, string ContrasenaNueva);