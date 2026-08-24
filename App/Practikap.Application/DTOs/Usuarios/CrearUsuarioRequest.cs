namespace Practikap.Application.DTOs.Usuarios;

/// <summary>
/// Alta de un usuario. Solo el Administrador puede enviarla (RF-02).
/// </summary>
/// <param name="RolId">Rol a asignar: 1 Administrador, 2 Instructor, 3 Aprendiz.</param>
/// <param name="Correo">Correo institucional, unico en el sistema.</param>
/// <param name="Contrasena">Contrasena inicial en claro. Se persiste solo su hash (RNF-05).</param>
/// <param name="Nombre">Nombres.</param>
/// <param name="Apellido">Apellidos.</param>
/// <param name="Telefono">Telefono de contacto. Opcional.</param>
public sealed record CrearUsuarioRequest
(
    int RolId,
    string Correo,
    string Contrasena,
    string Nombre,
    string Apellido,
    string? Telefono
);