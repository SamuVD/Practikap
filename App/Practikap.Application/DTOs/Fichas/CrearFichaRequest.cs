namespace Practikap.Application.DTOs.Fichas;

/// <summary>
/// Alta de una ficha de formacion. Solo el Administrador puede enviarla
/// (FA-26).
/// </summary>
/// <param name="NumeroFicha">Numero de la ficha, unico en el sistema. Por ejemplo "3168939".</param>
/// <param name="ProgramaId">Programa de formacion al que pertenece la ficha.</param>
public sealed record CrearFichaRequest
(
    string NumeroFicha,
    int ProgramaId
);
