using FluentValidation;
using Practikap.Application.DTOs.Usuarios;

namespace Practikap.Application.Validators.Usuarios;

/// <summary>Validacion de forma de <see cref="CambiarRolRequest"/> (RN-15).</summary>
public sealed class CambiarRolRequestValidator : AbstractValidator<CambiarRolRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public CambiarRolRequestValidator()
    {
        // Que el rol exista se comprueba en el caso de uso contra el catalogo:
        // aqui solo se descarta un valor imposible por forma.
        RuleFor(peticion => peticion.RolId)
            .GreaterThan(0).WithMessage("Debe indicar el rol destino.");
    }
}