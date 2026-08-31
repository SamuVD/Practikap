using Practikap.Domain.Entities;

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
    /// Lista las practicas cuyos identificadores se indican, sin restringir el
    /// alcance. Es la via por la que un caso de uso obtiene instancias rastreadas
    /// de practicas que ya selecciono por otro camino.
    /// </summary>
    /// <param name="ids">Identificadores de las practicas buscadas.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con las practicas encontradas.</returns>
    /// <remarks>
    /// A diferencia de los tres listados de alcance, este no lleva AsNoTracking y
    /// no carga el grafo, y las dos cosas son a proposito.
    ///
    /// Rastreado porque estas instancias se vinculan a un Reporte nuevo (M7). Una
    /// practica desatada que se agrega a la coleccion del reporte queda marcada
    /// Added y EF Core intentaria reinsertarla, con lo que la generacion de un
    /// reporte duplicaria filas en practicas en lugar de crear el vinculo.
    ///
    /// Sin grafo porque el consumidor ya tiene las practicas con sus navegaciones
    /// cargadas, traidas por el listado de alcance que resolvio el filtro: aqui
    /// solo hacen falta las entidades que el rastreador va a seguir. Incluir el
    /// grafo repetiria las mismas cinco tablas sin que nadie las leyera.
    ///
    /// No decide alcance: RN-13 se resuelve antes, en el listado que produjo estos
    /// identificadores (ADR-03).
    /// </remarks>
    Task<IReadOnlyList<Practica>> ListarPorIdsAsync(IEnumerable<int> ids, CancellationToken ct);

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

    /// <summary>Registra los cambios efectuados sobre una practica existente.</summary>
    /// <param name="practica">Practica modificada.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <remarks>
    /// El repositorio no invoca dominio (H28). La transicion de estado de RN-05,
    /// la reasignacion de RN-04 y la coherencia entre modalidad y empresa las
    /// decide la entidad, y el caso de uso es quien la invoca: es ahi donde vive
    /// IContextoUsuario, del que sale el indicador de Administrador (ADR-03).
    ///
    /// Sustituye a ActualizarEstadoAsync y ReasignarAsync, que hasta la Ronda 1
    /// llamaban a Practica.CambiarEstado y Practica.Reasignar desde dentro del
    /// repositorio. Un unico metodo cubre ademas CambiarModalidad, que aquellos
    /// dos no contemplaban.
    /// </remarks>
    Task ActualizarAsync(Practica practica, CancellationToken ct);
}
