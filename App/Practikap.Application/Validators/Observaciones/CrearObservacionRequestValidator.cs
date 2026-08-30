using FluentValidation;
using Practikap.Application.DTOs.Observaciones;

namespace Practikap.Application.Validators.Observaciones;

/// <summary>Validacion de forma de <see cref="CrearObservacionRequest"/> (RN-15).</summary>
/// <remarks>
/// Que el seguimiento exista, que no este anulado y que la practica a la que
/// pertenece admita registros son las tres puertas de I10, y viven en el caso de
/// uso. Aqui solo queda la presencia del contenido: la columna es TEXT y no
/// impone un tope que validar.
/// </remarks>
public sealed class CrearObservacionRequestValidator : AbstractValidator<CrearObservacionRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public CrearObservacionRequestValidator()
    {
        RuleFor(peticion => peticion.Contenido)
            .NotEmpty().WithMessage("El contenido de la observacion es obligatorio.");
    }
}
