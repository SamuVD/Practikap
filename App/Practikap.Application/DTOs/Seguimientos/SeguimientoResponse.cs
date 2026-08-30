using Practikap.Application.DTOs.Observaciones;

namespace Practikap.Application.DTOs.Seguimientos;

/// <summary>
/// Representacion de salida de un seguimiento, con sus observaciones anidadas.
/// </summary>
/// <remarks>
/// Las observaciones viajan dentro y no en una llamada aparte: es I5, y la
/// consulta que alimenta este DTO las trae en el mismo grafo.
///
/// Anulado y AnuladoPor salen siempre. Es I4: un registro anulado se devuelve
/// con su marca en lugar de desaparecer del historial, que es lo que hace
/// verificable la inmutabilidad de RN-12 desde fuera del sistema.
///
/// AnuladoPor es el identificador desnudo, sin nombre aplanado al lado. En M3,
/// H32 tuvo que aplanar instructor y aprendiz porque el grafo cargaba la entidad
/// Usuario entera, ContrasenaHash incluido. Aqui no hay tal riesgo: las
/// configuraciones de las dos tablas mapean anulado_por sin propiedad de
/// navegacion, de modo que ningun Usuario entra en el grafo y no hay nada que
/// proyectar mal (RNF-05).
///
/// PracticaId identifica a la practica, pero la practica no se expone: el grafo
/// la carga para resolver RN-13 y este DTO no la lee.
/// </remarks>
/// <param name="Id">Identificador del seguimiento.</param>
/// <param name="PracticaId">Practica a la que pertenece.</param>
/// <param name="Avance">Descripcion del avance observado.</param>
/// <param name="Etapa">Etapa de la practica a la que corresponde el avance.</param>
/// <param name="FechaRegistro">Momento del registro, determinado por el servidor (RN-11).</param>
/// <param name="Anulado">Marca de anulacion.</param>
/// <param name="AnuladoPor">Identificador del Administrador que anulo, nulo si el registro esta vigente.</param>
/// <param name="Observaciones">Observaciones asociadas, vigentes y anuladas.</param>
public sealed record SeguimientoResponse
(
    int Id,
    int PracticaId,
    string Avance,
    string Etapa,
    DateTime FechaRegistro,
    bool Anulado,
    int? AnuladoPor,
    IReadOnlyList<ObservacionResponse> Observaciones
);
