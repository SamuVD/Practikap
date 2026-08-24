namespace Practikap.Application.DTOs.Usuarios;

/// <summary>
/// Restablecimiento administrativo de contrasena (D8). No pide la contrasena
/// actual: existe precisamente porque el usuario la olvido.
/// </summary>
/// <param name="ContrasenaNueva">Contrasena de reemplazo que el Administrador asigna.</param>
public sealed record RestablecerContrasenaRequest(string ContrasenaNueva);