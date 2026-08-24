using FluentValidation;
using Practikap.Application.DTOs.Usuarios;

namespace Practikap.Application.Validators.Usuarios;

/// <summary>Validacion de forma de <see cref="CrearUsuarioRequest"/> (RN-15).</summary>
public sealed class CrearUsuarioRequestValidator : AbstractValidator<CrearUsuarioRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public CrearUsuarioRequestValidator()
    {
        RuleFor(peticion => peticion.RolId)
            .GreaterThan(0).WithMessage("Debe indicar el rol del usuario.");

        RuleFor(peticion => peticion.Correo)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El correo no tiene un formato valido.")
            .MaximumLength(180).WithMessage("El correo no puede superar 180 caracteres.");

        RuleFor(peticion => peticion.Contrasena).ConPoliticaDeContrasena();

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