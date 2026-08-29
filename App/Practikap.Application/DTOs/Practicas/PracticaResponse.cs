namespace Practikap.Application.DTOs.Practicas;

/// <summary>
/// Representacion de salida de una practica productiva.
/// </summary>
/// <remarks>
/// Va totalmente aplanado a proposito. La consulta que lo alimenta carga el
/// grafo completo, y con el la entidad Usuario entera de instructor y aprendiz,
/// que incluye ContrasenaHash: proyectar aqui el nombre y nada mas es lo que
/// impide que ese campo salga por la API (RNF-05). Ningun perfil de M3 declara
/// un CreateMap desde Usuario.
///
/// Los identificadores acompanan a cada nombre para que el frontend pueda
/// construir los enlaces sin una segunda consulta.
/// </remarks>
/// <param name="Id">Identificador de la practica.</param>
/// <param name="FichaId">Identificador de la ficha de formacion.</param>
/// <param name="NumeroFicha">Numero de la ficha de formacion.</param>
/// <param name="ProgramaId">Identificador del programa, derivado de la ficha.</param>
/// <param name="Programa">Nombre del programa de formacion.</param>
/// <param name="EmpresaId">Identificador de la empresa receptora, nulo en ProyectoProductivo y Monitoria.</param>
/// <param name="Empresa">Razon social de la empresa receptora, nula en ProyectoProductivo y Monitoria.</param>
/// <param name="InstructorId">Identificador del instructor responsable.</param>
/// <param name="Instructor">Nombre completo del instructor responsable.</param>
/// <param name="AprendizId">Identificador del aprendiz titular.</param>
/// <param name="Aprendiz">Nombre completo del aprendiz titular.</param>
/// <param name="Modalidad">Modalidad de la practica, como texto (H31).</param>
/// <param name="Estado">Estado dentro del ciclo de vida de RN-05, como texto (H31).</param>
/// <param name="FechaInicio">Fecha de inicio.</param>
/// <param name="FechaFin">Fecha de cierre, nula mientras la practica no se haya finalizado.</param>
/// <param name="FechaCreacion">Fecha de alta del registro.</param>
public sealed record PracticaResponse
(
    int Id,
    int FichaId,
    string NumeroFicha,
    int ProgramaId,
    string Programa,
    int? EmpresaId,
    string? Empresa,
    int InstructorId,
    string Instructor,
    int AprendizId,
    string Aprendiz,
    string Modalidad,
    string Estado,
    DateOnly FechaInicio,
    DateOnly? FechaFin,
    DateTime FechaCreacion
);
