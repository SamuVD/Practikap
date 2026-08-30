namespace Practikap.Application.DTOs.Practicas;

/// <summary>
/// Transicion de estado de una practica dentro del ciclo de vida de RN-05.
/// Solo el Administrador puede enviarla, incluido el retroceso (H17).
/// </summary>
/// <param name="Estado">
/// Estado destino: Pendiente, EnCurso, Finalizada o EnRiesgo (H31).
/// </param>
/// <param name="FechaFin">
/// Fecha de cierre. Solo se tiene en cuenta cuando el estado destino es
/// Finalizada: en ese caso el caso de uso invoca Practica.Finalizar en lugar de
/// Practica.CambiarEstado, que es la unica via por la que se escribe la fecha
/// de cierre en v1 (H30). Nunca anterior a la de inicio.
/// </param>
public sealed record CambiarEstadoPracticaRequest
(
    string Estado,
    DateOnly? FechaFin
);
