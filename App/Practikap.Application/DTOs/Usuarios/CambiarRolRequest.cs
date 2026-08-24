namespace Practikap.Application.DTOs.Usuarios;

/// <summary>
/// Reasignacion del rol de un usuario (RN-01).
/// </summary>
/// <param name="RolId">Rol destino: 1 Administrador, 2 Instructor, 3 Aprendiz.</param>
public sealed record CambiarRolRequest(int RolId);