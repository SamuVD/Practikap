namespace Practikap.Application.DTOs.Usuarios;

/// <summary>
/// Representacion de salida de un usuario. Nunca incluye el hash de la
/// contrasena (RNF-05).
/// </summary>
/// <param name="Id">Identificador del usuario.</param>
/// <param name="Correo">Correo institucional.</param>
/// <param name="Nombre">Nombres.</param>
/// <param name="Apellido">Apellidos.</param>
/// <param name="NombreCompleto">Nombre y apellido concatenados, para presentacion.</param>
/// <param name="Telefono">Telefono de contacto, si lo tiene.</param>
/// <param name="Rol">Nombre del rol: Administrador, Instructor o Aprendiz.</param>
/// <param name="Estado">Estado de la cuenta: Activo o Inactivo.</param>
/// <param name="FechaCreacion">Fecha de alta de la cuenta.</param>
public sealed record UsuarioResponse
(
    int Id,
    string Correo,
    string Nombre,
    string Apellido,
    string NombreCompleto,
    string? Telefono,
    string Rol,
    string Estado,
    DateTime FechaCreacion
);