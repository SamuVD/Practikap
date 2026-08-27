namespace Practikap.Application.DTOs.Autenticacion;

/// <summary>
/// Credenciales enviadas a POST /api/auth/login (CU-01).
/// </summary>
/// <param name="Correo">Correo institucional registrado.</param>
/// <param name="Contrasena">Contrasena en claro. Nunca se registra en el log (RNF-05).</param>
public sealed record LoginRequest(string Correo, string Contrasena);