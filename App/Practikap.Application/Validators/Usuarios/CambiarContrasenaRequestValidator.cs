using FluentValidation;
using Practikap.Application.DTOs.Usuarios;

namespace Practikap.Application.Validators.Usuarios;

/// <summary>Validacion de forma de <see cref="CambiarContrasenaRequest"/> (RN-15).</summary>
public sealed class CambiarContrasenaRequestValidator : AbstractValidator<CambiarContrasenaRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public CambiarContrasenaRequestValidator()
    {
        // La actual solo se exige presente: comprobar si es correcta ocurre en
        // el caso de uso y produce 401, no 400.
        RuleFor(peticion => peticion.ContrasenaActual)
            .NotEmpty().WithMessage("La contrasena actual es obligatoria.");

        RuleFor(peticion => peticion.ContrasenaNueva).ConPoliticaDeContrasena();

        RuleFor(peticion => peticion.ContrasenaNueva)
            .NotEqual(peticion => peticion.ContrasenaActual)
            .WithMessage("La contrasena nueva debe ser distinta de la actual.");
    }
}