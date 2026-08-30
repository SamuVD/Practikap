using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Mensaje"/>. Modulo M6.
/// </summary>
/// <remarks>
/// El contrato original declaraba MarcarLeidoAsync(int id, ct), que recibia el
/// identificador y obligaba al repositorio a cargar el mensaje y aplicarle
/// Mensaje.MarcarLeido. Eso es lo que H28 prohibio en M3, I9 extendio a M4 y J7
/// a M5: el repositorio no invoca dominio. Se reemplaza por el par que usan los
/// otros cinco repositorios, ObtenerPorIdAsync y ActualizarAsync, y la marca
/// pasa a aplicarla el caso de uso sobre la entidad rastreada.
/// </remarks>
public interface IMensajeRepository
{
    /// <summary>Obtiene un mensaje por su identificador.</summary>
    /// <param name="id">Identificador del mensaje.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El mensaje, o null si no existe.</returns>
    Task<Mensaje?> ObtenerPorIdAsync(int id, CancellationToken ct);

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

    /// <summary>Registra el cambio de estado de un mensaje ya existente.</summary>
    /// <param name="mensaje">Mensaje con la marca de lectura ya aplicada.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task ActualizarAsync(Mensaje mensaje, CancellationToken ct);

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
