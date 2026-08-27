using FluentValidation;
using Practikap.Application.DTOs.Autenticacion;

namespace Practikap.Application.Validators.Autenticacion;

/// <summary>
/// Validacion de forma de <see cref="LoginRequest"/> (RN-15). No comprueba si
/// las credenciales son correctas: eso ocurre en el caso de uso y produce 401,
/// no 400.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public LoginRequestValidator()
    {
        RuleFor(peticion => peticion.Correo)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El correo no tiene un formato valido.")
            .MaximumLength(180).WithMessage("El correo no puede superar 180 caracteres.");

        RuleFor(peticion => peticion.Contrasena)
            .NotEmpty().WithMessage("La contrasena es obligatoria.");
    }
}