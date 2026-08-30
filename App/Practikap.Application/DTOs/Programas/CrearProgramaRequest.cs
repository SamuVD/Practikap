namespace Practikap.Application.DTOs.Programas;

/// <summary>
/// Alta de un programa de formacion. Solo el Administrador puede enviarla
/// (FA-26).
/// </summary>
/// <param name="Nombre">Nombre del programa, unico en el sistema.</param>
/// <param name="Descripcion">Descripcion del programa. Opcional.</param>
public sealed record CrearProgramaRequest
(
    string Nombre,
    string? Descripcion
);
