using FluentValidation;
using Practikap.Application.DTOs.Practicas;

namespace Practikap.Application.Validators.Practicas;

/// <summary>Validacion de forma de <see cref="CrearPracticaRequest"/> (RN-15).</summary>
/// <remarks>
/// Que la ficha, la empresa y los participantes existan se comprueba en el caso
/// de uso contra sus repositorios: aqui solo se descartan valores imposibles por
/// forma. La coherencia entre modalidad y empresa que exige H22 tampoco se
/// valida aqui, sino en el constructor de Practica, que es su unica fuente.
/// </remarks>
public sealed class CrearPracticaRequestValidator : AbstractValidator<CrearPracticaRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public CrearPracticaRequestValidator()
    {
        RuleFor(peticion => peticion.FichaId)
            .GreaterThan(0).WithMessage("Debe indicar la ficha de formacion.");

        RuleFor(peticion => peticion.InstructorId)
            .GreaterThan(0).WithMessage("Debe indicar el instructor responsable.");

        RuleFor(peticion => peticion.AprendizId)
            .GreaterThan(0).WithMessage("Debe indicar el aprendiz titular.");

        RuleFor(peticion => peticion.AprendizId)
            .NotEqual(peticion => peticion.InstructorId)
                .WithMessage("El instructor y el aprendiz no pueden ser el mismo usuario.");

        RuleFor(peticion => peticion.Modalidad).ConModalidadValida();

        RuleFor(peticion => peticion.EmpresaId)
            .GreaterThan(0).WithMessage("La empresa indicada no es valida.")
            .When(peticion => peticion.EmpresaId is not null);

        RuleFor(peticion => peticion.FechaFin)
            .GreaterThanOrEqualTo(peticion => peticion.FechaInicio)
                .WithMessage("La fecha de finalizacion no puede ser anterior a la de inicio.")
            .When(peticion => peticion.FechaFin is not null);
    }
}
