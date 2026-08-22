using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Entrada del almacen clave/valor de configuracion general del sistema.
/// Provee, entre otros, el estado por defecto que RN-06 exige aplicar cuando
/// ninguna regla activa del Motor coincide.
/// </summary>
public class Configuracion
{
    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Configuracion() { }

    /// <summary>Crea una entrada de configuracion.</summary>
    /// <param name="clave">Clave unica, por ejemplo "estado_practica_por_defecto".</param>
    /// <param name="valor">Valor asociado a la clave.</param>
    /// <param name="actualizadoPor">Administrador responsable del valor (RF-09).</param>
    /// <param name="descripcion">Explicacion del proposito de la clave. Opcional.</param>
    /// <exception cref="ReglaDeDominioException">Si clave, valor o responsable son invalidos.</exception>
    public Configuracion(string clave, string valor, int actualizadoPor, string? descripcion = null)
    {
        if (string.IsNullOrWhiteSpace(clave))
            throw new ReglaDeDominioException("La clave de configuracion es obligatoria.");
        if (valor is null)
            throw new ReglaDeDominioException("El valor de configuracion es obligatorio.");
        if (actualizadoPor <= 0)
            throw new ReglaDeDominioException("La configuracion debe tener un responsable valido.");

        Clave = clave.Trim();
        Valor = valor;
        ActualizadoPor = actualizadoPor;
        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
    }

    /// <summary>Identificador. Columna configuracion.id.</summary>
    public int Id { get; private set; }

    /// <summary>Clave unica de la entrada. Columna configuracion.clave.</summary>
    public string Clave { get; private set; } = null!;

    /// <summary>Valor asociado. Columna configuracion.valor.</summary>
    public string Valor { get; private set; } = null!;

    /// <summary>Explicacion del proposito de la clave. Columna configuracion.descripcion.</summary>
    public string? Descripcion { get; private set; }

    /// <summary>Administrador responsable del valor actual. Columna configuracion.actualizado_por.</summary>
    public int ActualizadoPor { get; private set; }

    /// <summary>Fecha del ultimo cambio. La genera MySQL con ON UPDATE CURRENT_TIMESTAMP.</summary>
    public DateTime FechaActualizacion { get; private set; }

    /// <summary>Administrador que registro el valor actual.</summary>
    public Usuario Actualizador { get; private set; } = null!;

    /// <summary>
    /// Establece un nuevo valor para la clave y registra quien lo hizo.
    /// La clave nunca cambia: renombrarla equivaldria a crear otra entrada.
    /// </summary>
    /// <param name="valor">Nuevo valor.</param>
    /// <param name="actualizadoPorId">Administrador que ejecuta el cambio.</param>
    /// <exception cref="ReglaDeDominioException">Si el valor o el responsable son invalidos.</exception>
    public void Establecer(string valor, int actualizadoPorId)
    {
        if (valor is null)
            throw new ReglaDeDominioException("El valor de configuracion es obligatorio.");
        if (actualizadoPorId <= 0)
            throw new ReglaDeDominioException("La configuracion debe tener un responsable valido.");

        Valor = valor;
        ActualizadoPor = actualizadoPorId;
    }
}
