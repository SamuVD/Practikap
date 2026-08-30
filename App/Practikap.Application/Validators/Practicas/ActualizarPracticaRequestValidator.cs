using FluentValidation;
using Practikap.Application.DTOs.Practicas;

namespace Practikap.Application.Validators.Practicas;

/// <summary>Validacion de forma de <see cref="ActualizarPracticaRequest"/> (RN-15).</summary>
/// <remarks>
/// No declara reglas de fecha: H29 dejo la edicion de FechaInicio y FechaFin
/// fuera del alcance de esta operacion (FA-28).
/// </remarks>
public sealed class ActualizarPracticaRequestValidator : AbstractValidator<ActualizarPracticaRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public ActualizarPracticaRequestValidator()
    {
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
    }
}
