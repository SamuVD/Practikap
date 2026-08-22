using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Mensaje de la mensajeria interna. Depende de <see cref="Practica"/>: RN-13
/// impide que exista comunicacion entre usuarios que no comparten una practica.
/// </summary>
public class Mensaje
{
    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Mensaje() { }

    /// <summary>Envia un mensaje dentro del contexto de una practica.</summary>
    /// <param name="practicaId">Practica que enmarca la conversacion.</param>
    /// <param name="emisorId">Usuario que envia el mensaje.</param>
    /// <param name="receptorId">Usuario destinatario.</param>
    /// <param name="contenido">Texto del mensaje.</param>
    /// <exception cref="ReglaDeDominioException">
    /// Si falta la practica, si emisor y receptor coinciden o si el contenido viene vacio.
    /// </exception>
    public Mensaje(int practicaId, int emisorId, int receptorId, string contenido)
    {
        if (practicaId <= 0)
            throw new ReglaDeDominioException("El mensaje debe pertenecer a una practica valida.", "RN-13");
        if (emisorId <= 0 || receptorId <= 0)
            throw new ReglaDeDominioException("El mensaje requiere emisor y receptor validos.");
        if (emisorId == receptorId)
            throw new ReglaDeDominioException("Un usuario no puede enviarse mensajes a si mismo.");
        if (string.IsNullOrWhiteSpace(contenido))
            throw new ReglaDeDominioException("El contenido del mensaje es obligatorio.");

        PracticaId = practicaId;
        EmisorId = emisorId;
        ReceptorId = receptorId;
        Contenido = contenido.Trim();
        Leido = false;
    }

    /// <summary>Identificador. Columna mensajes.id.</summary>
    public int Id { get; private set; }

    /// <summary>Practica que enmarca la conversacion. Columna mensajes.practica_id.</summary>
    public int PracticaId { get; private set; }

    /// <summary>Usuario emisor. Columna mensajes.emisor_id.</summary>
    public int EmisorId { get; private set; }

    /// <summary>Usuario destinatario. Columna mensajes.receptor_id.</summary>
    public int ReceptorId { get; private set; }

    /// <summary>Texto del mensaje. Columna mensajes.contenido.</summary>
    public string Contenido { get; private set; } = null!;

    /// <summary>Momento del envio. La genera MySQL con DEFAULT CURRENT_TIMESTAMP.</summary>
    public DateTime FechaEnvio { get; private set; }

    /// <summary>Indica si el destinatario ya lo leyo. Columna mensajes.leido.</summary>
    public bool Leido { get; private set; }

    /// <summary>Practica que enmarca la conversacion.</summary>
    public Practica Practica { get; private set; } = null!;

    /// <summary>Marca el mensaje como leido por su destinatario.</summary>
    public void MarcarLeido() => Leido = true;
}
