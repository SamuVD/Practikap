namespace Practikap.Application.DTOs.Mensajes;

/// <summary>Representacion de salida de un mensaje de la mensajeria interna.</summary>
/// <remarks>
/// EmisorId y ReceptorId son identificadores desnudos, sin nombre aplanado al
/// lado. MensajeConfiguration mapea las dos claves foraneas con
/// HasOne&lt;Usuario&gt;().WithMany() y sin propiedad de navegacion, de modo que
/// ningun Usuario entra en el grafo y la fuga de ContrasenaHash que H32 vigila no
/// tiene por donde ocurrir (RNF-05).
///
/// Leido viaja siempre, tambien recien creado el mensaje, donde vale false. Es lo
/// que hace observable desde fuera el efecto del PATCH de K6.
/// </remarks>
/// <param name="Id">Identificador del mensaje.</param>
/// <param name="PracticaId">Practica que enmarca la conversacion.</param>
/// <param name="EmisorId">Usuario que envio el mensaje.</param>
/// <param name="ReceptorId">Usuario destinatario.</param>
/// <param name="Contenido">Texto del mensaje.</param>
/// <param name="FechaEnvio">Momento del envio, determinado por el servidor.</param>
/// <param name="Leido">Marca de lectura, que solo el receptor puede poner (K5).</param>
public sealed record MensajeResponse
(
    int Id,
    int PracticaId,
    int EmisorId,
    int ReceptorId,
    string Contenido,
    DateTime FechaEnvio,
    bool Leido
);
