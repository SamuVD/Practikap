using Practikap.Domain.Entities;
using Practikap.Domain.Enums;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="RegistroAuditoria"/>. Modulo M8.
/// </summary>
/// <remarks>
/// El contrato es de escritura y consulta unicamente: un asiento de bitacora no
/// se actualiza ni se elimina. Los parametros de entidad afectada y de accion son
/// las enumeraciones cerradas que fijan ADR-06 y el DDL, no texto libre.
///
/// No expone eliminacion, y es la decision F3 con la misma razon que en M7: borrar
/// un asiento destruye la unica evidencia de que la accion sensible ocurrio, que es
/// exactamente lo que la bitacora existe para conservar.
///
/// <see cref="ListarAsync"/> cambio de firma en el paso 4.9 (P6). La version del
/// scaffolding recibia <c>desde</c> y <c>hasta</c> <b>obligatorios</b>, de modo que
/// no habia manera de pedir la bitacora entera, y no admitia filtrar por actor, que
/// es la primera pregunta que un panel de auditoria hace. Los cinco criterios son
/// ahora opcionales y se combinan con Y logico.
/// </remarks>
public interface IAuditoriaRepository
{
    /// <summary>Registra un asiento en la bitacora de acciones sensibles.</summary>
    /// <param name="registro">Asiento a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct);

    /// <summary>Consulta la bitacora por los cinco criterios de P6, combinados con Y logico.</summary>
    /// <param name="entidadAfectada">Entidad por la que se filtra, o null para no filtrar.</param>
    /// <param name="accion">Tipo de accion por el que se filtra, o null para no filtrar.</param>
    /// <param name="usuarioId">Actor por el que se filtra, o null para no filtrar.</param>
    /// <param name="desde">Limite inferior del rango, inclusive, o null para no acotar.</param>
    /// <param name="hasta">Limite superior del rango, inclusive, o null para no acotar.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con los asientos que satisfacen el filtro, del mas reciente al mas antiguo.</returns>
    /// <remarks>
    /// Los cinco criterios se traducen a SQL y viajan al servidor, a diferencia de
    /// los nueve del filtro de M7, que O4 resolvio en memoria. La diferencia no es
    /// de gusto: alli habia un listado de alcance previo del que colgarse y las
    /// practicas de una institucion se cuentan en cientos; aqui no hay listado
    /// previo y la tabla crece con cada accion sensible del sistema, de modo que
    /// filtrar en memoria significaria traer la bitacora entera en cada consulta.
    /// </remarks>
    Task<IReadOnlyList<RegistroAuditoria>> ListarAsync(
        EntidadAuditada? entidadAfectada,
        AccionAuditoria? accion,
        int? usuarioId,
        DateTime? desde,
        DateTime? hasta,
        CancellationToken ct);
}
