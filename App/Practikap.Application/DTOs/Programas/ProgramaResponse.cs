namespace Practikap.Application.DTOs.Programas;

/// <summary>
/// Representacion de salida de un programa de formacion.
/// </summary>
/// <param name="Id">Identificador del programa.</param>
/// <param name="Nombre">Nombre del programa.</param>
/// <param name="Descripcion">Descripcion del programa. Puede venir nula.</param>
public sealed record ProgramaResponse
(
    int Id,
    string Nombre,
    string? Descripcion
);
