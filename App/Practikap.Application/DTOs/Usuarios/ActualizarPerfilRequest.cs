namespace Practikap.Application.DTOs.Usuarios;

/// <summary>
/// Datos personales editables de un usuario.
/// </summary>
/// <param name="Nombre">Nombres.</param>
/// <param name="Apellido">Apellidos.</param>
/// <param name="Telefono">Telefono de contacto. Opcional.</param>
/// <remarks>
/// No incluye Correo ni RolId. El correo es la credencial de acceso y cambiarlo
/// exigiria revalidar unicidad y revocar la sesion; el rol solo se modifica por
/// PATCH /api/usuarios/{id}/rol, reservado al Administrador (RN-01). Coincide
/// con mi-perfil.html, que muestra el rol deshabilitado.
/// </remarks>
public sealed record ActualizarPerfilRequest(string Nombre, string Apellido, string? Telefono);