using FluentValidation;
using Practikap.Application.DTOs.Programas;

namespace Practikap.Application.Validators.Programas;

/// <summary>Validacion de forma de <see cref="CrearProgramaRequest"/> (RN-15).</summary>
/// <remarks>
/// Que el nombre no este repetido se comprueba en el caso de uso contra
/// uq_programas_nombre: aqui solo se valida la forma.
/// </remarks>
public sealed class CrearProgramaRequestValidator : AbstractValidator<CrearProgramaRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public CrearProgramaRequestValidator()
    {
        RuleFor(peticion => peticion.Nombre)
            .NotEmpty().WithMessage("El nombre del programa es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");

        RuleFor(peticion => peticion.Descripcion)
            .MaximumLength(255).WithMessage("La descripcion no puede superar 255 caracteres.")
            .When(peticion => !string.IsNullOrWhiteSpace(peticion.Descripcion));
    }
}
