namespace Practikap.Application.DTOs.Reportes;

/// <summary>
/// Una practica dentro del contenido de un reporte (RF-08, O9).
/// </summary>
/// <remarks>
/// Va totalmente aplanado, por la misma razon que PracticaResponse: la consulta
/// que lo alimenta carga el grafo completo, y con el la entidad Usuario entera de
/// instructor y aprendiz, ContrasenaHash incluido. Proyectar aqui el nombre y
/// nada mas es lo que impide que ese campo salga por la API (RNF-05, H32).
/// Ningun perfil de M7 declara un CreateMap desde Usuario.
///
/// Lleva PracticaId pero no los identificadores de ficha, programa, empresa,
/// instructor y aprendiz, y ahi diverge de PracticaResponse, que si los lleva.
/// La razon es el destino de cada uno: aquella alimenta pantallas que construyen
/// enlaces, y esta es una fila de salida que FormateadorCsv escribe tal cual en el
/// archivo. Un identificador que nadie va a seguir seria una columna de ruido, y
/// las doce que quedan son exactamente las doce columnas del CSV, en este orden.
///
/// Los dos promedios son los del momento de la consulta, excluyen las anuladas
/// (J5) y salen del mismo calculo que informa GET /api/calificaciones. Un
/// promedio en cero significa "sin calificaciones computables" y no "mal
/// calificado": la practica sin calificar es el caso normal al principio.
/// </remarks>
/// <param name="PracticaId">Identificador de la practica consolidada.</param>
/// <param name="Aprendiz">Nombre completo del aprendiz titular.</param>
/// <param name="Instructor">Nombre completo del instructor responsable.</param>
/// <param name="Ficha">Numero de la ficha de formacion.</param>
/// <param name="Programa">Nombre del programa de formacion, derivado de la ficha.</param>
/// <param name="Empresa">Razon social de la empresa receptora, nula en ProyectoProductivo y Monitoria.</param>
/// <param name="Modalidad">Modalidad de la practica, como texto (H31).</param>
/// <param name="Estado">Estado dentro del ciclo de vida de RN-05, como texto (H31).</param>
/// <param name="FechaInicio">Fecha de inicio.</param>
/// <param name="FechaFin">Fecha de cierre, nula mientras la practica no se haya finalizado.</param>
/// <param name="PromedioInstructor">Promedio vigente de las calificaciones que emitio el Instructor. Cero si no hay ninguna computable.</param>
/// <param name="PromedioAprendiz">Promedio vigente de las calificaciones que emitio el Aprendiz. Cero si no hay ninguna computable.</param>
public sealed record LineaDeReporteResponse
(
    int PracticaId,
    string Aprendiz,
    string Instructor,
    string Ficha,
    string Programa,
    string? Empresa,
    string Modalidad,
    string Estado,
    DateOnly FechaInicio,
    DateOnly? FechaFin,
    decimal PromedioInstructor,
    decimal PromedioAprendiz
);
