using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Observacion asociada a un seguimiento. Depende de <see cref="Seguimiento"/>,
/// no de la practica directamente, y es inmutable por RN-12.
/// </summary>
public class Observacion
{
    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Observacion() { }

    /// <summary>Registra una observacion sobre un seguimiento.</summary>
    /// <param name="seguimientoId">Seguimiento al que se asocia.</param>
    /// <param name="contenido">Texto de la observacion.</param>
    /// <exception cref="ReglaDeDominioException">Si el seguimiento o el contenido son invalidos.</exception>
    public Observacion(int seguimientoId, string contenido)
    {
        if (seguimientoId <= 0)
            throw new ReglaDeDominioException("La observacion debe pertenecer a un seguimiento valido.");
        if (string.IsNullOrWhiteSpace(contenido))
            throw new ReglaDeDominioException("El contenido de la observacion es obligatorio.");

        SeguimientoId = seguimientoId;
        Contenido = contenido.Trim();
        Anulado = false;
    }

    /// <summary>Identificador. Columna observaciones.id.</summary>
    public int Id { get; private set; }

    /// <summary>Seguimiento al que pertenece. Columna observaciones.seguimiento_id.</summary>
    public int SeguimientoId { get; private set; }

    /// <summary>Texto de la observacion. Columna observaciones.contenido.</summary>
    public string Contenido { get; private set; } = null!;

    /// <summary>Momento del registro. La genera MySQL con DEFAULT CURRENT_TIMESTAMP (RN-11).</summary>
    public DateTime FechaRegistro { get; private set; }

    /// <summary>Marca de anulacion. Columna observaciones.anulado.</summary>
    public bool Anulado { get; private set; }

    /// <summary>Administrador que anulo el registro. Columna observaciones.anulado_por.</summary>
    public int? AnuladoPor { get; private set; }

    /// <summary>Seguimiento al que pertenece la observacion.</summary>
    public Seguimiento Seguimiento { get; private set; } = null!;

    /// <summary>
    /// Marca la observacion como anulada. Unica modificacion permitida por
    /// RN-12 y reservada al Administrador.
    /// </summary>
    /// <param name="anuladoPorId">Administrador que ejecuta la anulacion.</param>
    /// <exception cref="ReglaDeDominioException">Si ya estaba anulada o el actor es invalido.</exception>
    public void Anular(int anuladoPorId)
    {
        if (anuladoPorId <= 0)
            throw new ReglaDeDominioException("La anulacion requiere un actor valido.", "RN-12");
        if (Anulado)
            throw new ReglaDeDominioException("La observacion ya se encuentra anulada.", "RN-12");

        Anulado = true;
        AnuladoPor = anuladoPorId;
    }
}
