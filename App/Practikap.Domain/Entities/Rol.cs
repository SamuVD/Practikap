using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Rol del sistema. Catalogo estable con tres valores sembrados por el
/// Script_DDL.sql: Administrador, Instructor y Aprendiz (RF-02).
/// La aplicacion lo consulta pero no crea roles nuevos durante la operacion.
/// </summary>
public class Rol
{
    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Rol() { }

    /// <summary>Crea un rol del catalogo.</summary>
    /// <param name="nombre">Nombre unico del rol.</param>
    /// <param name="descripcion">Alcance y responsabilidades del rol.</param>
    public Rol(string nombre, string descripcion)
    {
        Nombre = string.IsNullOrWhiteSpace(nombre)
            ? throw new ReglaDeDominioException("El nombre del rol es obligatorio.")
            : nombre.Trim();
        Descripcion = descripcion?.Trim() ?? string.Empty;
    }

    /// <summary>Identificador. Columna roles.id.</summary>
    public int Id { get; private set; }

    /// <summary>Nombre unico del rol. Columna roles.nombre.</summary>
    public string Nombre { get; private set; } = null!;

    /// <summary>Alcance y responsabilidades del rol. Columna roles.descripcion.</summary>
    public string Descripcion { get; private set; } = null!;
}
