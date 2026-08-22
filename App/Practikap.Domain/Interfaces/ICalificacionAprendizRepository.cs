using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="CalificacionAprendiz"/>. Modulo M5, direccion
/// Aprendiz hacia Instructor.
/// </summary>
/// <remarks>
/// Este repositorio nunca consulta al de la direccion contraria: RN-10 exige
/// que ambas calificaciones sean independientes entre si.
/// </remarks>
public interface ICalificacionAprendizRepository
{
    /// <summary>Obtiene una calificacion por su identificador.</summary>
    /// <param name="id">Identificador de la calificacion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La calificacion, o null si no existe.</returns>
    Task<CalificacionAprendiz?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Lista las calificaciones registradas sobre una practica.</summary>
    /// <param name="practicaId">Practica consultada.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con las calificaciones de la practica.</returns>
    Task<IReadOnlyList<CalificacionAprendiz>> ListarPorPracticaAsync(int practicaId, CancellationToken ct);

    /// <summary>
    /// Calcula el promedio de las calificaciones no anuladas de una practica.
    /// Es el insumo numerico que el caso de uso entrega al Motor de Reglas para
    /// evaluar el umbral de riesgo (RN-09).
    /// </summary>
    /// <param name="practicaId">Practica consultada.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El promedio vigente, o cero si no hay calificaciones computables.</returns>
    Task<decimal> PromedioVigenteAsync(int practicaId, CancellationToken ct);

    /// <summary>
    /// Registra una calificacion nueva. La marca de tiempo la fija el servidor
    /// conforme a RN-11.
    /// </summary>
    /// <param name="calificacion">Calificacion a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado a la calificacion.</returns>
    Task<int> AgregarAsync(CalificacionAprendiz calificacion, CancellationToken ct);

    /// <summary>
    /// Marca una calificacion como anulada. Unica alteracion permitida por
    /// RN-12, reservada al Administrador.
    /// </summary>
    /// <param name="id">Identificador de la calificacion.</param>
    /// <param name="anuladoPorId">Administrador que ejecuta la anulacion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task AnularAsync(int id, int anuladoPorId, CancellationToken ct);
}
