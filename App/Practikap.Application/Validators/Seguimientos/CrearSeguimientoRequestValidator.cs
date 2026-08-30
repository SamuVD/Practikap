using FluentValidation;
using Practikap.Application.DTOs.Seguimientos;

namespace Practikap.Application.Validators.Seguimientos;

/// <summary>Validacion de forma de <see cref="CrearSeguimientoRequest"/> (RN-15).</summary>
/// <remarks>
/// Aqui solo se descartan valores imposibles por forma. Que la practica exista,
/// que este En curso o En riesgo (I2) y que el solicitante sea su instructor
/// (I7) se comprueba en el caso de uso, contra el repositorio y contra
/// IContextoUsuario.
///
/// A diferencia de los validadores de M3, este no usa ReglasDeEnumerado: M4 no
/// expone ningun enumerado. Etapa es texto libre y no un catalogo, de modo que
/// lo unico verificable sobre ella es el ancho de la columna.
/// </remarks>
public sealed class CrearSeguimientoRequestValidator : AbstractValidator<CrearSeguimientoRequest>
{
    /// <summary>Ancho de la columna seguimientos.etapa en el Script_DDL.sql.</summary>
    private const int LargoMaximoEtapa = 100;

    /// <summary>Declara las reglas de validacion.</summary>
    public CrearSeguimientoRequestValidator()
    {
        RuleFor(peticion => peticion.PracticaId)
            .GreaterThan(0).WithMessage("Debe indicar la practica sobre la que se registra el avance.");

        // La columna es TEXT: no hay tope de forma que imponer, solo presencia.
        RuleFor(peticion => peticion.Avance)
            .NotEmpty().WithMessage("El avance del seguimiento es obligatorio.");

        RuleFor(peticion => peticion.Etapa)
            .NotEmpty().WithMessage("La etapa del seguimiento es obligatoria.")
            .MaximumLength(LargoMaximoEtapa)
                .WithMessage($"La etapa no puede superar los {LargoMaximoEtapa} caracteres.");
    }
}
