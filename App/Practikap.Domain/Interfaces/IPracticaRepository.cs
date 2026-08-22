using Practikap.Domain.Entities;
using Practikap.Domain.Enums;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Practica"/>. Modulo M3.
/// Los tres metodos de listado corresponden a los tres alcances de RN-13: el
/// caso de uso elige cual invocar segun el rol autenticado (ADR-03).
/// </summary>
public interface IPracticaRepository
{
    /// <summary>Obtiene una practica por su identificador.</summary>
    /// <param name="id">Identificador de la practica.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La practica, o null si no existe.</returns>
    Task<Practica?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Lista las practicas a cargo de un instructor. Alcance Asignado de RN-13.</summary>
    /// <param name="instructorId">Instructor responsable.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con las practicas del instructor.</returns>
    Task<IReadOnlyList<Practica>> ListarPorInstructorAsync(int instructorId, CancellationToken ct);

    /// <summary>Lista las practicas de un aprendiz. Alcance Propio de RN-13.</summary>
    /// <param name="aprendizId">Aprendiz titular.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con las practicas del aprendiz.</returns>
    Task<IReadOnlyList<Practica>> ListarPorAprendizAsync(int aprendizId, CancellationToken ct);

    /// <summary>Lista todas las practicas. Alcance Global de RN-13, reservado al Administrador.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con todas las practicas.</returns>
    Task<IReadOnlyList<Practica>> ListarTodasAsync(CancellationToken ct);

    /// <summary>
    /// Indica si el aprendiz ya tiene una practica sin finalizar. Implementa la
    /// verificacion previa exigida por RN-04, que no puede resolverse con un
    /// indice unico porque MySQL no admite indices unicos parciales.
    /// </summary>
    /// <param name="aprendizId">Identificador del aprendiz a verificar.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>true si ya existe una practica activa; false en caso contrario.</returns>
    Task<bool> TieneActivaAsync(int aprendizId, CancellationToken ct);

    /// <summary>Registra una practica nueva.</summary>
    /// <param name="practica">Practica a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado a la practica.</returns>
    Task<int> AgregarAsync(Practica practica, CancellationToken ct);

    /// <summary>
    /// Persiste el estado de una practica. La validez de la transicion la
    /// decide la entidad segun RN-05; el repositorio solo escribe el resultado.
    /// </summary>
    /// <param name="id">Identificador de la practica.</param>
    /// <param name="estado">Estado ya validado por el dominio.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task ActualizarEstadoAsync(int id, EstadoPractica estado, CancellationToken ct);

    /// <summary>
    /// Persiste la reasignacion de instructor y aprendiz, accion que RN-04
    /// reserva al Administrador.
    /// </summary>
    /// <param name="id">Identificador de la practica.</param>
    /// <param name="instructorId">Nuevo instructor responsable.</param>
    /// <param name="aprendizId">Nuevo aprendiz titular.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task ReasignarAsync(int id, int instructorId, int aprendizId, CancellationToken ct);
}
