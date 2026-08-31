using FluentValidation;
using Practikap.Application.DTOs.Reglas;

namespace Practikap.Application.Validators.Reglas;

/// <summary>Validacion de forma de <see cref="CrearReglaRequest"/> (RN-15).</summary>
/// <remarks>
/// Aqui solo se descartan valores imposibles por forma. Que el campo evaluado y la
/// accion resultante esten en sus listas blancas se comprueba en el caso de uso,
/// contra ReglasDeMotor, porque N1 y N2 fijan 422 para esos dos y toda
/// ValidationException se traduce a 400.
///
/// Las tres cotas de longitud son los anchos fisicos de las columnas del DDL, no
/// cotas de producto: nombre VARCHAR(150), campo_evaluado VARCHAR(100) y
/// accion_resultante VARCHAR(255). Sin ellas el rechazo llegaria desde MySQL como
/// un fallo no controlado en lugar de un 400 con detalle por campo.
///
/// Que el nombre no este vacio lo exige tambien el constructor de Regla, que es su
/// unica fuente. Se repite aqui a proposito: el constructor da un 422 sin decir que
/// campo fallo, y este validador da un 400 que los nombra todos de una vez.
/// </remarks>
public sealed class CrearReglaRequestValidator : AbstractValidator<CrearReglaRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public CrearReglaRequestValidator()
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

        // La columna es INT UNSIGNED y el constructor de Regla rechaza los
        // negativos por RN-07.
        RuleFor(peticion => peticion.Prioridad)
            .GreaterThanOrEqualTo(0).WithMessage("La prioridad no puede ser negativa.");
    }
}
