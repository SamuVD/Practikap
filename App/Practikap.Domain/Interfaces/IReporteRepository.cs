using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Reporte"/>. Modulo M7.
/// </summary>
/// <remarks>
/// La tabla puente reporte_practica no tiene repositorio propio ni clase propia:
/// se persiste como la coleccion de navegacion <see cref="Reporte.Practicas"/>,
/// dentro del mismo SaveChanges que registra el reporte.
///
/// El contrato no ofrece metodo de eliminacion ni ninguno que reciba el contenido
/// de un reporte ya generado: un reporte es un rastro de lo que se consulto en un
/// momento dado, y editarlo o borrarlo destruiria la unica evidencia de que la
/// consulta ocurrio. La ausencia de esos metodos es la evidencia verificable de
/// que la traza se conserva (F3).
///
/// Este contrato tenia dos metodos que O5 y O4 retiraron, y no por gusto:
///
/// RegistrarAsync(reporte, practicaIds, ct) recibia identificadores, de modo que
/// para armar el vinculo el repositorio tenia que cargar las practicas e invocar
/// <see cref="Reporte.VincularPractica"/>. Eso es dominio invocado desde la
/// Infraestructura, que es justo lo que H28 descarto en M3, I9 aplico en M4, J7
/// extendio a M5, L8 a M6 y N8 a M2. AgregarAsync(reporte, ct) recibe el agregado
/// ya compuesto: quien lo compone es el caso de uso, que es donde vive el dominio.
///
/// ConsolidarAsync(filtro, alcance, ct) metia el filtro y el alcance dentro del
/// repositorio. El filtro es lo que H27 saco de IPracticaRepository en M3, y el
/// alcance lo resuelve el caso de uso sobre IContextoUsuario (ADR-03, RN-13). M7
/// no necesita ninguno de los dos aqui: reusa los listados de
/// <see cref="IPracticaRepository"/> y filtra en memoria, igual que
/// ListarPracticasUseCase.
///
/// Los dos listados que si quedan son los dos alcances que O3 deja vivos sobre
/// este modulo: Global para el Administrador y Asignado para el Instructor, que
/// solo ve los reportes que genero el mismo. El Aprendiz no accede a M7 y por eso
/// no hay un tercer listado.
/// </remarks>
public interface IReporteRepository
{
    /// <summary>
    /// Registra un reporte ya compuesto, con sus practicas vinculadas. La marca de
    /// tiempo la fija el servidor conforme a RN-11.
    /// </summary>
    /// <param name="reporte">Reporte a persistir, con su coleccion de practicas ya poblada.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado al reporte.</returns>
    Task<int> AgregarAsync(Reporte reporte, CancellationToken ct);

    /// <summary>Obtiene un reporte por su identificador, con sus practicas consolidadas.</summary>
    /// <param name="id">Identificador del reporte.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El reporte, o null si no existe.</returns>
    /// <remarks>
    /// Carga la coleccion Practicas con su grafo: ficha, programa, empresa,
    /// instructor y aprendiz. Sin el, la consulta de un reporte no podria
    /// recomponer su contenido y solo devolveria el rastro (O14).
    /// </remarks>
    Task<Reporte?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Lista el historico completo de reportes. Alcance Global de RN-13.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con todos los reportes generados.</returns>
    Task<IReadOnlyList<Reporte>> ListarTodosAsync(CancellationToken ct);

    /// <summary>Lista los reportes que genero un usuario. Alcance Asignado de RN-13.</summary>
    /// <param name="generadoPorId">Usuario que genero los reportes.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con los reportes de ese generador.</returns>
    Task<IReadOnlyList<Reporte>> ListarPorGeneradorAsync(int generadoPorId, CancellationToken ct);
}
