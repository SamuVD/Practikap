namespace Practikap.Application.DTOs.Mensajes;

/// <summary>
/// Datos de entrada para enviar un mensaje dentro del contexto de una practica.
/// </summary>
/// <remarks>
/// Dos campos y ninguno mas, que es K2 entero. El emisor sale de IContextoUsuario
/// y el receptor se deriva como el otro participante de la practica. Declararlos
/// aqui los habria hecho falsificables: cualquiera podria escribir mensajes en
/// nombre de otro, o mandarselos a un usuario con el que no comparte nada.
///
/// Tampoco declara ningun campo de fecha ni la marca de lectura. La primera la
/// genera MySQL con DEFAULT CURRENT_TIMESTAMP; la segunda nace en false y solo el
/// receptor la cambia, por una via distinta (K5, K6).
/// </remarks>
/// <param name="PracticaId">Practica que enmarca la conversacion.</param>
/// <param name="Contenido">Texto del mensaje.</param>
public sealed record EnviarMensajeRequest
(
    int PracticaId,
    string Contenido
);
