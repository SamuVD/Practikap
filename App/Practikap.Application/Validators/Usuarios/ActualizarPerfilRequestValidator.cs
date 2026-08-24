using FluentValidation;
using Practikap.Application.DTOs.Usuarios;

namespace Practikap.Application.Validators.Usuarios;

/// <summary>Validacion de forma de <see cref="ActualizarPerfilRequest"/> (RN-15).</summary>
public sealed class ActualizarPerfilRequestValidator : AbstractValidator<ActualizarPerfilRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public ActualizarPerfilRequestValidator()
    {
        RuleFor(peticion => peticion.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");

        RuleFor(peticion => peticion.Apellido)
            .NotEmpty().WithMessage("El apellido es obligatorio.")
            .MaximumLength(150).WithMessage("El apellido no puede superar 150 caracteres.");

        RuleFor(peticion => peticion.Telefono)
            .MaximumLength(20).WithMessage("El telefono no puede superar 20 caracteres.")
            .When(peticion => !string.IsNullOrWhiteSpace(peticion.Telefono));
    }
}