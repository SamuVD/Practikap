namespace Practikap.Application.DTOs.Practicas;

/// <summary>
/// Reasignacion de participantes y cambio de modalidad de una practica. Solo el
/// Administrador puede enviarla (RF-03, H17).
/// </summary>
/// <remarks>
/// H29 acota el alcance de esta operacion: no edita fechas. El dominio solo
/// escribe FechaFin desde Practica.Finalizar, y siempre acompanada de una
/// transicion de estado, que es la via de H30. La edicion de FechaInicio y
/// FechaFin queda diferida a v2 (FA-28).
/// </remarks>
/// <param name="InstructorId">Nuevo instructor responsable (RN-04).</param>
/// <param name="AprendizId">Nuevo aprendiz titular (RN-04).</param>
/// <param name="Modalidad">
/// Modalidad de la practica: ContratoDeAprendizaje, Pasantia, ProyectoProductivo
/// o Monitoria (H31).
/// </param>
/// <param name="EmpresaId">
/// Empresa receptora. Obligatoria salvo en ProyectoProductivo y Monitoria, que
/// no la admiten (H22, H25).
/// </param>
public sealed record ActualizarPracticaRequest
(
    int InstructorId,
    int AprendizId,
    string Modalidad,
    int? EmpresaId
);
