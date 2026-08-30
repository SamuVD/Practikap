using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Regla"/>. Modulo M2, Motor de Reglas.
/// </summary>
/// <remarks>
/// El paso 3.1 declaraba un CambiarEstadoAsync que recibia el identificador y
/// habria obligado al repositorio a cargar la regla y llamarle Activar o
/// Desactivar, es decir a invocar dominio. Se elimino en favor de
/// ObtenerPorIdAsync y ActualizarAsync, que el contrato ya traia, y la activacion
/// la aplica el caso de uso (N8). Extiende a M2 el criterio de H28, I9, J7 y de
/// las decisiones equivalentes de las series K y L.
/// </remarks>
public interface IReglaRepository
{
    /// <summary>
    /// Lista las reglas activas ordenadas por prioridad ascendente. Es el
    /// insumo que el caso de uso entrega al Motor para que RN-07 pueda aplicar
    /// una sola regla de forma determinista.
    /// </summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con las reglas activas ordenadas.</returns>
    Task<IReadOnlyList<Regla>> ListarActivasOrdenadasAsync(CancellationToken ct);

    /// <summary>Lista todas las reglas, activas e inactivas, para el panel de administracion.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con todas las reglas.</returns>
    Task<IReadOnlyList<Regla>> ListarAsync(CancellationToken ct);

    /// <summary>Obtiene una regla por su identificador.</summary>
    /// <param name="id">Identificador de la regla.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La regla, o null si no existe.</returns>
    Task<Regla?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Registra una regla nueva.</summary>
    /// <param name="regla">Regla a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado a la regla.</returns>
    Task<int> AgregarAsync(Regla regla, CancellationToken ct);

    /// <summary>
    /// Registra los cambios efectuados sobre una regla existente, incluido el de
    /// su activacion. Materializa RN-08: una regla entra o sale de operacion sin
    /// modificar codigo ni desplegar de nuevo.
    /// </summary>
    /// <param name="regla">Regla modificada.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task ActualizarAsync(Regla regla, CancellationToken ct);
}
