namespace Practikap.Application.DTOs.Reportes;

/// <summary>
/// Criterios de seleccion de practicas para generar un reporte (RF-08). Forma de
/// entrada de <see cref="Domain.ValueObjects.FiltroReporte"/>.
/// </summary>
/// <remarks>
/// Los nueve criterios son opcionales y se combinan con Y logico. Un filtro sin
/// ningun criterio selecciona todas las practicas que el alcance del solicitante
/// permita ver (RN-13), que es la misma semantica que describe
/// FiltroReporte.EstaVacio.
///
/// Estado y Modalidad son texto y no el enumerado, con el criterio de H31: el
/// contrato de la API es el nombre exacto del miembro. Un literal desconocido no
/// se rechaza aqui sino en el caso de uso, con 422 (O19): no es un error de forma
/// sino una solicitud que el sistema no puede procesar, igual que en
/// ListarPracticasUseCase.
///
/// El filtro que se persiste no es este DTO sino el objeto de valor traducido, y
/// se persiste aunque el reporte no llegue a generarse: es el rastro de que se
/// pregunto.
/// </remarks>
/// <param name="InstructorId">Instructor responsable de las practicas buscadas. Null no filtra.</param>
/// <param name="AprendizId">Aprendiz titular de las practicas buscadas. Null no filtra.</param>
/// <param name="FichaId">Ficha de formacion a la que pertenecen las practicas. Null no filtra.</param>
/// <param name="ProgramaId">Programa de formacion, resuelto a traves de la ficha. Null no filtra.</param>
/// <param name="EmpresaId">Empresa donde se desarrolla la practica. Null no filtra.</param>
/// <param name="Estado">Estado exacto de la practica, como texto (H31). Null no filtra.</param>
/// <param name="Modalidad">Modalidad exacta de la practica, como texto (H31). Null no filtra.</param>
/// <param name="Desde">Limite inferior del rango de fecha de inicio, inclusive. Null no filtra.</param>
/// <param name="Hasta">Limite superior del rango de fecha de inicio, inclusive. Null no filtra.</param>
public sealed record FiltroReporteRequest
(
    int? InstructorId,
    int? AprendizId,
    int? FichaId,
    int? ProgramaId,
    int? EmpresaId,
    string? Estado,
    string? Modalidad,
    DateOnly? Desde,
    DateOnly? Hasta
);
