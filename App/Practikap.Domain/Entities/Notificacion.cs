using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Notificacion dirigida a un usuario. Raiz de agregado que pertenece a su
/// destinatario y que puede originarse en una regla del Motor (RN-09).
/// </summary>
public class Notificacion
{
    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Notificacion() { }

    /// <summary>Crea una notificacion originada en una accion del sistema.</summary>
    /// <param name="usuarioId">Usuario destinatario.</param>
    /// <param name="tipo">Origen funcional de la notificacion.</param>
    /// <param name="contenido">Texto que se muestra al destinatario.</param>
    /// <exception cref="ReglaDeDominioException">Si el destinatario o el contenido son invalidos.</exception>
    public Notificacion(int usuarioId, TipoNotificacion tipo, string contenido)
    {
        if (usuarioId <= 0)
            throw new ReglaDeDominioException("La notificacion requiere un destinatario valido.");
        if (string.IsNullOrWhiteSpace(contenido))
            throw new ReglaDeDominioException("El contenido de la notificacion es obligatorio.");

        UsuarioId = usuarioId;
        Tipo = tipo;
        Contenido = contenido.Trim();
        Leida = false;
    }

    /// <summary>Identificador. Columna notificaciones.id.</summary>
    public int Id { get; private set; }

    /// <summary>Usuario destinatario. Columna notificaciones.usuario_id.</summary>
    public int UsuarioId { get; private set; }

    /// <summary>Regla que la disparo, si el origen fue el Motor. Columna notificaciones.regla_id.</summary>
    public int? ReglaId { get; private set; }

    /// <summary>Origen funcional. Columna notificaciones.tipo.</summary>
    public TipoNotificacion Tipo { get; private set; }

    /// <summary>Texto mostrado al destinatario. Columna notificaciones.contenido.</summary>
    public string Contenido { get; private set; } = null!;

    /// <summary>Indica si el destinatario ya la leyo. Columna notificaciones.leida.</summary>
    public bool Leida { get; private set; }

    /// <summary>Momento de la generacion. La genera MySQL con DEFAULT CURRENT_TIMESTAMP.</summary>
    public DateTime FechaGeneracion { get; private set; }

    /// <summary>Regla que disparo la notificacion, cuando el origen fue el Motor.</summary>
    public Regla? Regla { get; private set; }

    /// <summary>
    /// Crea una notificacion disparada por el Motor de Reglas, dejando trazado
    /// que regla la origino tal como exige RN-09.
    /// </summary>
    /// <param name="usuarioId">Usuario destinatario, normalmente el instructor.</param>
    /// <param name="reglaId">Regla activa que produjo la alerta.</param>
    /// <param name="contenido">Texto de la alerta.</param>
    /// <returns>La notificacion lista para persistir.</returns>
    /// <exception cref="ReglaDeDominioException">Si el destinatario, la regla o el contenido son invalidos.</exception>
    public static Notificacion DesdeRegla(int usuarioId, int reglaId, string contenido)
    {
        if (reglaId <= 0)
            throw new ReglaDeDominioException("La notificacion del Motor requiere una regla valida.", "RN-09");

        var notificacion = new Notificacion(usuarioId, TipoNotificacion.Riesgo, contenido)
        {
            ReglaId = reglaId
        };

        return notificacion;
    }

    /// <summary>Marca la notificacion como leida por su destinatario.</summary>
    public void MarcarLeida() => Leida = true;
}
