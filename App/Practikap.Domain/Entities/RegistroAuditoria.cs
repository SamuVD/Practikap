using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Asiento de la bitacora de acciones sensibles del sistema: anulaciones
/// (RN-12), retrocesos de estado (RN-05), cambios de rol (RN-01) y
/// configuracion del Motor (RN-08).
/// </summary>
/// <remarks>
/// Segun ADR-06 la entidad no declara propiedades de navegacion: la referencia
/// al objeto afectado es polimorfica y no tiene clave foranea fisica, por lo
/// que se expresa como el par <see cref="EntidadAfectada"/> y
/// <see cref="EntidadId"/>. El asiento es inmutable una vez creado: una
/// bitacora que se puede editar no es una bitacora.
/// </remarks>
public class RegistroAuditoria
{
    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private RegistroAuditoria() { }

    /// <summary>Crea un asiento de auditoria.</summary>
    /// <param name="usuarioId">Actor que ejecuta la accion.</param>
    /// <param name="entidadAfectada">Entidad sobre la que se actuo.</param>
    /// <param name="entidadId">Identificador del registro afectado.</param>
    /// <param name="accion">Tipo de accion ejecutada.</param>
    /// <param name="detalle">Descripcion complementaria. Opcional.</param>
    /// <exception cref="ReglaDeDominioException">Si el actor o el registro afectado son invalidos.</exception>
    public RegistroAuditoria(int usuarioId, EntidadAuditada entidadAfectada, int entidadId,
                             AccionAuditoria accion, string? detalle = null)
    {
        if (usuarioId <= 0)
            throw new ReglaDeDominioException("La auditoria requiere un actor valido.");
        if (entidadId <= 0)
            throw new ReglaDeDominioException("La auditoria requiere el identificador del registro afectado.");

        UsuarioId = usuarioId;
        EntidadAfectada = entidadAfectada;
        EntidadId = entidadId;
        Accion = accion;
        Detalle = string.IsNullOrWhiteSpace(detalle) ? null : detalle.Trim();
    }

    /// <summary>Identificador. Columna auditoria.id.</summary>
    public int Id { get; private set; }

    /// <summary>Actor que ejecuto la accion. Columna auditoria.usuario_id.</summary>
    public int UsuarioId { get; private set; }

    /// <summary>Entidad sobre la que se actuo. Columna auditoria.entidad_afectada.</summary>
    public EntidadAuditada EntidadAfectada { get; private set; }

    /// <summary>Identificador del registro afectado. Columna auditoria.entidad_id.</summary>
    public int EntidadId { get; private set; }

    /// <summary>Tipo de accion ejecutada. Columna auditoria.accion.</summary>
    public AccionAuditoria Accion { get; private set; }

    /// <summary>Descripcion complementaria. Columna auditoria.detalle.</summary>
    public string? Detalle { get; private set; }

    /// <summary>Momento del asiento. La genera MySQL con DEFAULT CURRENT_TIMESTAMP.</summary>
    public DateTime FechaRegistro { get; private set; }
}
