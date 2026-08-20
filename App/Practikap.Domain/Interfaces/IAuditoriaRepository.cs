using Practikap.Domain.Entities;
using Practikap.Domain.Enums;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="RegistroAuditoria"/>. Modulo M8.
/// </summary>
/// <remarks>
/// El contrato es de escritura y consulta unicamente: un asiento de bitacora no
/// se actualiza ni se elimina. El parametro de entidad afectada es la
/// enumeracion cerrada que fija ADR-06, no texto libre.
/// </remarks>
public interface IAuditoriaRepository
{
    /// <summary>Registra un asiento en la bitacora de acciones sensibles.</summary>
    /// <param name="registro">Asiento a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct);

    /// <summary>Consulta la bitacora por entidad afectada y rango de fechas.</summary>
    /// <param name="entidadAfectada">Entidad por la que se filtra, o null para no filtrar.</param>
    /// <param name="desde">Limite inferior del rango, inclusive.</param>
    /// <param name="hasta">Limite superior del rango, inclusive.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con los asientos que satisfacen el filtro.</returns>
    Task<IReadOnlyList<RegistroAuditoria>> ListarAsync(EntidadAuditada? entidadAfectada, DateTime desde, DateTime hasta, CancellationToken ct);
}
