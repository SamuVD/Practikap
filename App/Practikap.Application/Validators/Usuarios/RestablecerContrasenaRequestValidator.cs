using FluentValidation;
using Practikap.Application.DTOs.Usuarios;

namespace Practikap.Application.Validators.Usuarios;

/// <summary>Validacion de forma de <see cref="RestablecerContrasenaRequest"/> (RN-15).</summary>
public sealed class RestablecerContrasenaRequestValidator
    : AbstractValidator<RestablecerContrasenaRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public RestablecerContrasenaRequestValidator()
    {
        RuleFor(peticion => peticion.ContrasenaNueva).ConPoliticaDeContrasena();
    }
}