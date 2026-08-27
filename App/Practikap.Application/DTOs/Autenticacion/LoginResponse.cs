using Practikap.Application.DTOs.Usuarios;

namespace Practikap.Application.DTOs.Autenticacion;

/// <summary>
/// Sesion iniciada. Incluye los datos del usuario para que el frontend resuelva
/// el tablero destino segun el rol sin una segunda llamada (CU-01).
/// </summary>
/// <param name="Token">JWT firmado, para la cabecera Authorization: Bearer.</param>
/// <param name="ExpiraEn">Momento de expiracion en UTC.</param>
/// <param name="Usuario">Datos del usuario autenticado.</param>
public sealed record LoginResponse(string Token, DateTime ExpiraEn, UsuarioResponse Usuario);