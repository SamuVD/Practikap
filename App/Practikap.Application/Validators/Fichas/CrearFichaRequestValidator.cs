using FluentValidation;
using Practikap.Application.DTOs.Fichas;

namespace Practikap.Application.Validators.Fichas;

/// <summary>Validacion de forma de <see cref="CrearFichaRequest"/> (RN-15).</summary>
/// <remarks>
/// Que el programa exista y que el numero no este repetido se comprueban en el
/// caso de uso contra sus repositorios: aqui solo se valida la forma.
/// </remarks>
public sealed class CrearFichaRequestValidator : AbstractValidator<CrearFichaRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public CrearFichaRequestValidator()
    {
        RuleFor(peticion => peticion.NumeroFicha)
            .NotEmpty().WithMessage("El numero de ficha es obligatorio.")
            .MaximumLength(20).WithMessage("El numero de ficha no puede superar 20 caracteres.");

        RuleFor(peticion => peticion.ProgramaId)
            .GreaterThan(0).WithMessage("Debe indicar el programa de formacion.");
    }
}
