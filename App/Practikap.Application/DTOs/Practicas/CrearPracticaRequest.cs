namespace Practikap.Application.DTOs.Practicas;

/// <summary>
/// Alta de una practica productiva. Solo el Administrador puede enviarla
/// (RF-03, H17).
/// </summary>
/// <param name="FichaId">Ficha de formacion del aprendiz.</param>
/// <param name="InstructorId">Instructor responsable. Su cuenta debe estar activa y tener rol Instructor (H7).</param>
/// <param name="AprendizId">Aprendiz titular. Su cuenta debe estar activa y tener rol Aprendiz (H7).</param>
/// <param name="Modalidad">
/// Modalidad de la practica: ContratoDeAprendizaje, Pasantia, ProyectoProductivo
/// o Monitoria (H31).
/// </param>
/// <param name="FechaInicio">Fecha de inicio de la practica.</param>
/// <param name="EmpresaId">
/// Empresa receptora. Obligatoria salvo en ProyectoProductivo y Monitoria, que
/// no la admiten (H22, H25).
/// </param>
/// <param name="FechaFin">Fecha de cierre prevista. Opcional, nunca anterior a la de inicio.</param>
public sealed record CrearPracticaRequest
(
    int FichaId,
    int InstructorId,
    int AprendizId,
    string Modalidad,
    DateOnly FechaInicio,
    int? EmpresaId,
    DateOnly? FechaFin
);
