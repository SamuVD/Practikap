namespace Practikap.Application.DTOs.Calificaciones;

/// <summary>
/// Las calificaciones de una practica en sus dos direcciones, cada una con su
/// promedio vigente. Salida de GET /api/calificaciones?practicaId=.
/// </summary>
/// <remarks>
/// Las dos direcciones viajan en listas separadas y no en un array unico con un
/// campo direccion: RN-10 dice que son registros independientes, y separarlas en
/// la propia forma del JSON hace visible esa independencia sin tener que leer la
/// documentacion. Es la misma razon por la que el Script_DDL.sql uso dos tablas
/// en lugar de una con discriminador.
///
/// Los dos promedios excluyen las anuladas (J5) y los calcula
/// PromedioVigenteAsync, que es el mismo metodo que el Motor de Reglas va a
/// consultar en el paso 4.7 para evaluar el umbral de riesgo (RN-09). Calcularlos
/// aqui sobre las listas ya traidas habria duplicado la definicion de promedio
/// vigente en dos lugares que despues podrian divergir.
///
/// Un promedio vale cero cuando su direccion no tiene ninguna calificacion
/// computable. Cero no significa "mal calificado" sino "sin datos": las listas
/// que van al lado son las que permiten distinguir los dos casos.
/// </remarks>
/// <param name="PracticaId">Practica consultada.</param>
/// <param name="PromedioInstructor">Promedio vigente de las calificaciones que emitio el Instructor.</param>
/// <param name="PromedioAprendiz">Promedio vigente de las calificaciones que emitio el Aprendiz.</param>
/// <param name="DelInstructor">Calificaciones que el Instructor emitio sobre el Aprendiz, vigentes y anuladas.</param>
/// <param name="DelAprendiz">Calificaciones que el Aprendiz emitio sobre el Instructor, vigentes y anuladas.</param>
public sealed record CalificacionesDePracticaResponse
(
    int PracticaId,
    decimal PromedioInstructor,
    decimal PromedioAprendiz,
    IReadOnlyList<CalificacionResponse> DelInstructor,
    IReadOnlyList<CalificacionResponse> DelAprendiz
);
