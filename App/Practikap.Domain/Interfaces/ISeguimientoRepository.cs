using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Seguimiento"/>. Modulo M4.
/// </summary>
/// <remarks>
/// El contrato no ofrece metodo de actualizacion ni de eliminacion: el
/// historial es inmutable por RN-12 y la unica alteracion admitida es la marca
/// de anulacion. La ausencia de esos metodos es la evidencia verificable de
/// que la regla se cumple a nivel arquitectonico.
/// </remarks>
public interface ISeguimientoRepository
{
    /// <summary>Obtiene un seguimiento por su identificador.</summary>
    /// <param name="id">Identificador del seguimiento.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El seguimiento, o null si no existe.</returns>
    Task<Seguimiento?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Lista el historial de seguimientos de una practica.</summary>
    /// <param name="practicaId">Practica cuyo historial se consulta.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con los seguimientos de la practica.</returns>
    Task<IReadOnlyList<Seguimiento>> ListarPorPracticaAsync(int practicaId, CancellationToken ct);

    /// <summary>
    /// Registra un seguimiento nuevo. La marca de tiempo la fija el servidor,
    /// no el cliente, conforme a RN-11.
    /// </summary>
    /// <param name="seguimiento">Seguimiento a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado al seguimiento.</returns>
    Task<int> AgregarAsync(Seguimiento seguimiento, CancellationToken ct);

    /// <summary>
    /// Marca un seguimiento como anulado. Unica alteracion del historial que
    /// RN-12 permite, y solo al Administrador.
    /// </summary>
    /// <param name="id">Identificador del seguimiento.</param>
    /// <param name="anuladoPorId">Administrador que ejecuta la anulacion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task AnularAsync(int id, int anuladoPorId, CancellationToken ct);

    /// <summary>
    /// Devuelve la fecha del ultimo seguimiento registrado en una practica.
    /// Es insumo del Motor de Reglas para evaluar inactividad.
    /// </summary>
    /// <param name="practicaId">Practica consultada.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La fecha del ultimo registro, o null si no hay ninguno.</returns>
    Task<DateTime?> FechaUltimoRegistroAsync(int practicaId, CancellationToken ct);
}
