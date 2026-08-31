using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="CalificacionAprendiz"/>. Modulo M5, direccion
/// Aprendiz hacia Instructor.
/// </summary>
/// <remarks>
/// Este repositorio nunca consulta al de la direccion contraria: RN-10 exige
/// que ambas calificaciones sean independientes entre si.
///
/// El contrato no ofrece metodo de eliminacion ni ninguno que reciba el valor o
/// el comentario de un registro ya existente: la calificacion es inmutable por
/// RN-12 y la unica alteracion admitida es la marca de anulacion, que solo el
/// Dominio sabe aplicar.
///
/// ActualizarAsync no contradice lo anterior: no lleva datos, solo registra una
/// entidad que llego desatada. Quien decide que cambia es
/// <see cref="CalificacionAprendiz.Anular"/>, invocado desde el caso de uso.
/// J7 lo puso en lugar de AnularAsync(id, anuladoPorId, ct), que obligaba al
/// repositorio a cargar la entidad e invocar dominio. Extiende a M5 lo que H28
/// decidio en M3 e I9 aplico en M4.
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
    /// Calcula de una sola vez el promedio vigente de varias practicas. Es el
    /// insumo con el que M7 compone el contenido de un reporte (RF-08).
    /// </summary>
    /// <param name="practicaIds">Practicas consultadas.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>
    /// Diccionario de practica a promedio. Una practica sin calificaciones
    /// computables no aparece: el consumidor resuelve la ausencia como cero.
    /// </returns>
    /// <remarks>
    /// Existe para no invocar PromedioVigenteAsync en un bucle. Un reporte grupal
    /// puede consolidar decenas de practicas, y una consulta por cada una seria
    /// el problema N+1 en el camino mas caliente del modulo. Aqui es un unico
    /// GROUP BY que MySQL resuelve en el servidor.
    ///
    /// Aplica el mismo redondeo que PromedioVigenteAsync y la misma exclusion de
    /// las anuladas (J5), de modo que el numero que informa un reporte es el mismo
    /// que informa GET /api/calificaciones para la misma practica.
    ///
    /// La ausencia sustituye al cero que devuelve PromedioVigenteAsync, y no es
    /// una divergencia de contrato: un GROUP BY no produce grupos vacios, y
    /// fabricar la fila en cero aqui obligaria a recorrer los identificadores de
    /// entrada una segunda vez para nada. El consumidor ya distingue los dos casos.
    ///
    /// Como todo lo demas en este contrato, no consulta la direccion contraria
    /// (RN-10): el reporte pide los dos diccionarios por separado y los junta al
    /// componer cada linea.
    /// </remarks>
    Task<IReadOnlyDictionary<int, decimal>> PromediosPorPracticasAsync(
        IEnumerable<int> practicaIds, CancellationToken ct);

    /// <summary>
    /// Registra una calificacion nueva. La marca de tiempo la fija el servidor
    /// conforme a RN-11.
    /// </summary>
    /// <param name="calificacion">Calificacion a persistir.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado a la calificacion.</returns>
    Task<int> AgregarAsync(CalificacionAprendiz calificacion, CancellationToken ct);

    /// <summary>
    /// Registra una calificacion que llega desatada. Es la via por la que se
    /// persiste la marca de anulacion, unica alteracion que RN-12 permite y
    /// reservada al Administrador.
    /// </summary>
    /// <param name="calificacion">Calificacion ya modificada por el Dominio.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task ActualizarAsync(CalificacionAprendiz calificacion, CancellationToken ct);
}
