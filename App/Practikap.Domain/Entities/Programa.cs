using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Programa de formacion del SENA. Raiz de agregado gestionada desde el modulo
/// M8 y consultada desde M3 a traves de <see cref="Ficha"/>.
/// </summary>
public class Programa
{
    private readonly List<Ficha> _fichas = [];

    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Programa() { }

    /// <summary>Crea un programa de formacion.</summary>
    /// <param name="nombre">Nombre unico del programa.</param>
    /// <param name="descripcion">Descripcion del programa. Opcional.</param>
    /// <exception cref="ReglaDeDominioException">Si el nombre viene vacio.</exception>
    public Programa(string nombre, string? descripcion = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaDeDominioException("El nombre del programa es obligatorio.");

        Nombre = nombre.Trim();
        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
    }

    /// <summary>Identificador. Columna programas.id.</summary>
    public int Id { get; private set; }

    /// <summary>Nombre unico del programa. Columna programas.nombre.</summary>
    public string Nombre { get; private set; } = null!;

    /// <summary>Descripcion del programa. Columna programas.descripcion.</summary>
    public string? Descripcion { get; private set; }

    /// <summary>Fichas que pertenecen a este programa.</summary>
    public IReadOnlyCollection<Ficha> Fichas => _fichas;

    /// <summary>Actualiza los datos del programa.</summary>
    /// <param name="nombre">Nuevo nombre.</param>
    /// <param name="descripcion">Nueva descripcion. Opcional.</param>
    /// <exception cref="ReglaDeDominioException">Si el nombre viene vacio.</exception>
    public void Actualizar(string nombre, string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaDeDominioException("El nombre del programa es obligatorio.");

        Nombre = nombre.Trim();
        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
    }
}
