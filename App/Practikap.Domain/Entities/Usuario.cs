using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Usuario de la plataforma. Raiz del agregado del modulo M1 y origen de la
/// identidad que gobiernan RN-01 y RN-13.
/// </summary>
public class Usuario
{
    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Usuario() { }

    /// <summary>Crea un usuario con los datos minimos que lo hacen valido.</summary>
    /// <param name="rolId">Rol asignado. Determina los permisos (RF-02).</param>
    /// <param name="correo">Correo institucional, unico en el sistema.</param>
    /// <param name="contrasenaHash">Hash BCrypt de la contrasena. Nunca texto plano (RNF-05).</param>
    /// <param name="nombre">Nombres del usuario.</param>
    /// <param name="apellido">Apellidos del usuario.</param>
    /// <param name="telefono">Telefono de contacto. Opcional.</param>
    /// <exception cref="ReglaDeDominioException">Si algun dato obligatorio falta o es invalido.</exception>
    public Usuario(int rolId, string correo, string contrasenaHash,
                   string nombre, string apellido, string? telefono = null)
    {
        if (rolId <= 0)
            throw new ReglaDeDominioException("El usuario debe tener un rol asignado.");
        if (string.IsNullOrWhiteSpace(correo))
            throw new ReglaDeDominioException("El correo es obligatorio.");
        if (string.IsNullOrWhiteSpace(contrasenaHash))
            throw new ReglaDeDominioException("La contrasena es obligatoria.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaDeDominioException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(apellido))
            throw new ReglaDeDominioException("El apellido es obligatorio.");

        RolId = rolId;
        Correo = correo.Trim().ToLowerInvariant();
        ContrasenaHash = contrasenaHash;
        Nombre = nombre.Trim();
        Apellido = apellido.Trim();
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
        Estado = EstadoUsuario.Activo;
    }

    /// <summary>Identificador. Columna usuarios.id.</summary>
    public int Id { get; private set; }

    /// <summary>Rol asignado. Columna usuarios.rol_id.</summary>
    public int RolId { get; private set; }

    /// <summary>Correo institucional, unico. Columna usuarios.correo.</summary>
    public string Correo { get; private set; } = null!;

    /// <summary>Hash BCrypt de la contrasena. Columna usuarios.contrasena_hash.</summary>
    public string ContrasenaHash { get; private set; } = null!;

    /// <summary>Nombres. Columna usuarios.nombre.</summary>
    public string Nombre { get; private set; } = null!;

    /// <summary>Apellidos. Columna usuarios.apellido.</summary>
    public string Apellido { get; private set; } = null!;

    /// <summary>Telefono de contacto. Columna usuarios.telefono.</summary>
    public string? Telefono { get; private set; }

    /// <summary>Estado de habilitacion de la cuenta. Columna usuarios.estado.</summary>
    public EstadoUsuario Estado { get; private set; }

    /// <summary>
    /// Fecha de alta. La genera MySQL con DEFAULT CURRENT_TIMESTAMP; el dominio
    /// nunca la asigna, en linea con el criterio de RN-11.
    /// </summary>
    public DateTime FechaCreacion { get; private set; }

    /// <summary>
    /// Fecha de la ultima modificacion. La genera MySQL con
    /// ON UPDATE CURRENT_TIMESTAMP; el dominio nunca la asigna.
    /// </summary>
    public DateTime FechaActualizacion { get; private set; }

    /// <summary>Rol al que pertenece el usuario.</summary>
    public Rol Rol { get; private set; } = null!;

    /// <summary>Nombre completo para presentacion.</summary>
    public string NombreCompleto => $"{Nombre} {Apellido}";

    /// <summary>Indica si la cuenta puede iniciar sesion.</summary>
    public bool EstaActivo => Estado == EstadoUsuario.Activo;

    /// <summary>
    /// Cambia el rol del usuario. Operacion reservada al Administrador segun
    /// RN-01; la verificacion del rol solicitante ocurre en la capa de API.
    /// </summary>
    /// <param name="rolId">Nuevo rol a asignar.</param>
    /// <exception cref="ReglaDeDominioException">Si el rol es invalido.</exception>
    public void CambiarRol(int rolId)
    {
        if (rolId <= 0)
            throw new ReglaDeDominioException("El rol indicado no es valido.", "RN-01");
        RolId = rolId;
    }

    /// <summary>Reemplaza el hash de la contrasena.</summary>
    /// <param name="nuevoHash">Nuevo hash BCrypt. Nunca la contrasena en claro (RNF-05).</param>
    /// <exception cref="ReglaDeDominioException">Si el hash viene vacio.</exception>
    public void CambiarContrasena(string nuevoHash)
    {
        if (string.IsNullOrWhiteSpace(nuevoHash))
            throw new ReglaDeDominioException("La nueva contrasena es obligatoria.");
        ContrasenaHash = nuevoHash;
    }

    /// <summary>Actualiza los datos personales de contacto.</summary>
    /// <param name="nombre">Nombres.</param>
    /// <param name="apellido">Apellidos.</param>
    /// <param name="telefono">Telefono de contacto. Opcional.</param>
    /// <exception cref="ReglaDeDominioException">Si nombre o apellido vienen vacios.</exception>
    public void ActualizarDatos(string nombre, string apellido, string? telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaDeDominioException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(apellido))
            throw new ReglaDeDominioException("El apellido es obligatorio.");

        Nombre = nombre.Trim();
        Apellido = apellido.Trim();
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
    }

    /// <summary>Habilita la cuenta para iniciar sesion.</summary>
    public void Activar() => Estado = EstadoUsuario.Activo;

    /// <summary>Deshabilita la cuenta sin eliminar su historial.</summary>
    public void Desactivar() => Estado = EstadoUsuario.Inactivo;
}
