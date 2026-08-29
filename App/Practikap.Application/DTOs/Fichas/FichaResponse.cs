namespace Practikap.Application.DTOs.Fichas;

/// <summary>
/// Representacion de salida de una ficha de formacion.
/// </summary>
/// <param name="Id">Identificador de la ficha.</param>
/// <param name="NumeroFicha">Numero de la ficha.</param>
/// <param name="ProgramaId">Identificador del programa al que pertenece.</param>
/// <param name="Programa">Nombre del programa al que pertenece.</param>
public sealed record FichaResponse
(
    int Id,
    string NumeroFicha,
    int ProgramaId,
    string Programa
);
