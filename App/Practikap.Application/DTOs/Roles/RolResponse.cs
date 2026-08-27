namespace Practikap.Application.DTOs.Roles;

/// <summary>
/// Rol del catalogo. Alimenta el selector de rol de gestion-usuarios.html (D6).
/// </summary>
/// <param name="Id">Identificador del rol.</param>
/// <param name="Nombre">Nombre unico: Administrador, Instructor o Aprendiz.</param>
/// <param name="Descripcion">Alcance y responsabilidades del rol.</param>
public sealed record RolResponse(int Id, string Nombre, string Descripcion);