using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Empresa externa que recibe al aprendiz. Raiz de agregado opcional en la
/// practica: solo la modalidad Proyecto productivo puede prescindir de ella.
/// </summary>
public class Empresa
{
    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Empresa() { }

    /// <summary>Crea una empresa receptora.</summary>
    /// <param name="razonSocial">Razon social de la empresa.</param>
    /// <param name="nit">NIT unico de la empresa.</param>
    /// <param name="jefeInmediatoNombre">Nombre del jefe inmediato. Opcional.</param>
    /// <param name="jefeInmediatoCorreo">Correo del jefe inmediato. Opcional.</param>
    /// <param name="jefeInmediatoTelefono">Telefono del jefe inmediato. Opcional.</param>
    /// <exception cref="ReglaDeDominioException">Si razon social o NIT vienen vacios.</exception>
    public Empresa(string razonSocial, string nit,
                   string? jefeInmediatoNombre = null,
                   string? jefeInmediatoCorreo = null,
                   string? jefeInmediatoTelefono = null)
    {
        if (string.IsNullOrWhiteSpace(razonSocial))
            throw new ReglaDeDominioException("La razon social es obligatoria.");
        if (string.IsNullOrWhiteSpace(nit))
            throw new ReglaDeDominioException("El NIT es obligatorio.");

        RazonSocial = razonSocial.Trim();
        Nit = nit.Trim();
        JefeInmediatoNombre = Normalizar(jefeInmediatoNombre);
        JefeInmediatoCorreo = Normalizar(jefeInmediatoCorreo);
        JefeInmediatoTelefono = Normalizar(jefeInmediatoTelefono);
    }

    /// <summary>Identificador. Columna empresas.id.</summary>
    public int Id { get; private set; }

    /// <summary>Razon social. Columna empresas.razon_social.</summary>
    public string RazonSocial { get; private set; } = null!;

    /// <summary>NIT unico. Columna empresas.nit.</summary>
    public string Nit { get; private set; } = null!;

    /// <summary>Nombre del jefe inmediato. Columna empresas.jefe_inmediato_nombre.</summary>
    public string? JefeInmediatoNombre { get; private set; }

    /// <summary>Correo del jefe inmediato. Columna empresas.jefe_inmediato_correo.</summary>
    public string? JefeInmediatoCorreo { get; private set; }

    /// <summary>Telefono del jefe inmediato. Columna empresas.jefe_inmediato_telefono.</summary>
    public string? JefeInmediatoTelefono { get; private set; }

    /// <summary>Actualiza la razon social de la empresa.</summary>
    /// <param name="razonSocial">Nueva razon social.</param>
    /// <exception cref="ReglaDeDominioException">Si viene vacia.</exception>
    public void ActualizarRazonSocial(string razonSocial)
    {
        if (string.IsNullOrWhiteSpace(razonSocial))
            throw new ReglaDeDominioException("La razon social es obligatoria.");
        RazonSocial = razonSocial.Trim();
    }

    /// <summary>Actualiza los datos de contacto del jefe inmediato.</summary>
    /// <param name="nombre">Nombre del jefe inmediato. Opcional.</param>
    /// <param name="correo">Correo del jefe inmediato. Opcional.</param>
    /// <param name="telefono">Telefono del jefe inmediato. Opcional.</param>
    public void ActualizarContacto(string? nombre, string? correo, string? telefono)
    {
        JefeInmediatoNombre = Normalizar(nombre);
        JefeInmediatoCorreo = Normalizar(correo);
        JefeInmediatoTelefono = Normalizar(telefono);
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
