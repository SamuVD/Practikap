using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Mensaje"/>. Modulo M6.
/// </summary>
public interface IMensajeRepository
{
    /// <summary>Lista los mensajes intercambiados en una practica.</summary>
    /// <param name="practicaId">Practica que enmarca la conversacion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con los mensajes de la practica.</returns>
    Task<IReadOnlyList<Mensaje>> ListarPorPracticaAsync(int practicaId, CancellationToken ct);

    /// <summary>Registra un mensaje nuevo.</summary>
    /// <param name="mensaje">Mensaje a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado al mensaje.</returns>
    Task<int> AgregarAsync(Mensaje mensaje, CancellationToken ct);

    /// <summary>Marca un mensaje como leido por su destinatario.</summary>
    /// <param name="id">Identificador del mensaje.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task MarcarLeidoAsync(int id, CancellationToken ct);

    /// <summary>
    /// Indica si dos usuarios comparten al menos una practica sin finalizar.
    /// Es el hecho que el caso de uso necesita para aplicar RN-13 y rechazar
    /// la mensajeria entre usuarios sin vinculo. El repositorio responde el
    /// hecho; la decision de rechazar la toma el caso de uso.
    /// </summary>
    /// <param name="emisorId">Usuario que pretende enviar el mensaje.</param>
    /// <param name="receptorId">Usuario destinatario.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>true si comparten una practica activa; false en caso contrario.</returns>
    Task<bool> CompartenPracticaActivaAsync(int emisorId, int receptorId, CancellationToken ct);
}
