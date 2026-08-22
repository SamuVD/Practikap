using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Registro de avance de una practica. Entidad dependiente de
/// <see cref="Practica"/> e inmutable por RN-12: su contenido no se edita ni
/// se elimina, y lo unico que admite es la marca de anulacion del Administrador.
/// </summary>
public class Seguimiento
{
    private readonly List<Observacion> _observaciones = [];

    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Seguimiento() { }

    /// <summary>Registra un seguimiento sobre una practica.</summary>
    /// <param name="practicaId">Practica sobre la que se registra el avance.</param>
    /// <param name="avance">Descripcion del avance observado.</param>
    /// <param name="etapa">Etapa de la practica a la que corresponde el avance.</param>
    /// <exception cref="ReglaDeDominioException">Si la practica, el avance o la etapa son invalidos.</exception>
    public Seguimiento(int practicaId, string avance, string etapa)
    {
        if (practicaId <= 0)
            throw new ReglaDeDominioException("El seguimiento debe pertenecer a una practica valida.");
        if (string.IsNullOrWhiteSpace(avance))
            throw new ReglaDeDominioException("El avance del seguimiento es obligatorio.");
        if (string.IsNullOrWhiteSpace(etapa))
            throw new ReglaDeDominioException("La etapa del seguimiento es obligatoria.");

        PracticaId = practicaId;
        Avance = avance.Trim();
        Etapa = etapa.Trim();
        Anulado = false;
    }

    /// <summary>Identificador. Columna seguimientos.id.</summary>
    public int Id { get; private set; }

    /// <summary>Practica a la que pertenece. Columna seguimientos.practica_id.</summary>
    public int PracticaId { get; private set; }

    /// <summary>Descripcion del avance. Columna seguimientos.avance.</summary>
    public string Avance { get; private set; } = null!;

    /// <summary>Etapa de la practica. Columna seguimientos.etapa.</summary>
    public string Etapa { get; private set; } = null!;

    /// <summary>
    /// Momento del registro. La genera MySQL con DEFAULT CURRENT_TIMESTAMP,
    /// nunca el cliente, tal como exige RN-11.
    /// </summary>
    public DateTime FechaRegistro { get; private set; }

    /// <summary>Marca de anulacion. Columna seguimientos.anulado.</summary>
    public bool Anulado { get; private set; }

    /// <summary>Administrador que anulo el registro. Columna seguimientos.anulado_por.</summary>
    public int? AnuladoPor { get; private set; }

    /// <summary>Practica a la que pertenece el seguimiento.</summary>
    public Practica Practica { get; private set; } = null!;

    /// <summary>Observaciones asociadas a este seguimiento.</summary>
    public IReadOnlyCollection<Observacion> Observaciones => _observaciones;

    /// <summary>
    /// Marca el seguimiento como anulado. Es la unica modificacion que RN-12
    /// permite sobre el historial, y esta reservada al Administrador; la
    /// verificacion del rol ocurre en la capa de Aplicacion.
    /// </summary>
    /// <param name="anuladoPorId">Administrador que ejecuta la anulacion.</param>
    /// <exception cref="ReglaDeDominioException">Si el registro ya estaba anulado o el actor es invalido.</exception>
    public void Anular(int anuladoPorId)
    {
        if (anuladoPorId <= 0)
            throw new ReglaDeDominioException("La anulacion requiere un actor valido.", "RN-12");
        if (Anulado)
            throw new ReglaDeDominioException("El seguimiento ya se encuentra anulado.", "RN-12");

        Anulado = true;
        AnuladoPor = anuladoPorId;
    }
}
