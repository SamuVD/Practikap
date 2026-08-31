using FluentValidation;
using Practikap.Application.DTOs.Reglas;

namespace Practikap.Application.Validators.Reglas;

/// <summary>Validacion de forma de <see cref="ActualizarReglaRequest"/> (RN-15).</summary>
/// <remarks>
/// Declara las mismas reglas que CrearReglaRequestValidator, porque el PUT
/// reemplaza la definicion entera y no admite menos que el alta. Se escribe aparte
/// y no se hereda del otro: los dos validan DTOs distintos, y FluentValidation los
/// resuelve por tipo. Es el mismo reparto que M3 tiene entre
/// CrearPracticaRequestValidator y ActualizarPracticaRequestValidator.
///
/// Lo que el PUT no valida es la activacion, que este DTO no trae: Regla.Actualizar
/// no toca Activa, de modo que editar una regla retirada la deja retirada (N7).
/// </remarks>
public sealed class ActualizarReglaRequestValidator : AbstractValidator<ActualizarReglaRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public ActualizarReglaRequestValidator()
    {
        RuleFor(peticion => peticion.Nombre)
            .NotEmpty().WithMessage("El nombre de la regla es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder los 150 caracteres.");

        RuleFor(peticion => peticion.CampoEvaluado)
            .NotEmpty().WithMessage("El campo evaluado es obligatorio.")
            .MaximumLength(100).WithMessage("El campo evaluado no puede exceder los 100 caracteres.");

        RuleFor(peticion => peticion.Operador).ConOperadorValido();

        // DECIMAL(6,2): cuatro digitos enteros y dos decimales.
        RuleFor(peticion => peticion.ValorCondicion)
            .PrecisionScale(6, 2, ignoreTrailingZeros: true)
                .WithMessage("El valor de la condicion admite hasta cuatro enteros y dos decimales.");

        RuleFor(peticion => peticion.AccionResultante)
            .NotEmpty().WithMessage("La accion resultante es obligatoria.")
            .MaximumLength(255).WithMessage("La accion resultante no puede exceder los 255 caracteres.");

        // La columna es INT UNSIGNED y Regla.Actualizar rechaza los negativos por
        // RN-07.
        RuleFor(peticion => peticion.Prioridad)
            .GreaterThanOrEqualTo(0).WithMessage("La prioridad no puede ser negativa.");
    }
}
