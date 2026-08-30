using FluentValidation;
using Practikap.Application.DTOs.Mensajes;

namespace Practikap.Application.Validators.Mensajes;

/// <summary>Validacion de forma de <see cref="EnviarMensajeRequest"/> (RN-15).</summary>
/// <remarks>
/// El contenido vacio se comprueba aqui y no solo en el Dominio porque CU-06 y
/// HU-08 piden 400 para ese caso, y una ReglaDeDominioException se traduce a 422.
/// La guarda del constructor de Mensaje queda como respaldo del camino que no
/// pasa por este validador.
///
/// Aqui solo se descarta lo imposible por forma. Que la practica exista, que este
/// En curso o En riesgo (K3) y que el solicitante sea uno de sus dos participantes
/// se comprueba en el caso de uso, contra el repositorio y contra
/// IContextoUsuario.
///
/// No usa ReglasDeEnumerado: M6 no expone ningun enumerado en su entrada.
/// </remarks>
public sealed class EnviarMensajeRequestValidator : AbstractValidator<EnviarMensajeRequest>
{
    /// <summary>
    /// Tope de longitud del contenido (K10). Es una cota de producto y no el
    /// limite fisico de la columna: contenido es TEXT y admite unos 16383
    /// caracteres en utf8mb4, pero la mensajeria de Practikap es un canal interno
    /// entre instructor y aprendiz, no un campo de redaccion larga.
    /// </summary>
    private const int LongitudMaximaDelContenido = 2000;

    /// <summary>Declara las reglas de validacion.</summary>
    public EnviarMensajeRequestValidator()
    {
        RuleFor(peticion => peticion.PracticaId)
            .GreaterThan(0).WithMessage("Debe indicar la practica en la que se envia el mensaje.");

        RuleFor(peticion => peticion.Contenido)
            .NotEmpty().WithMessage("El contenido del mensaje es obligatorio.")
            .MaximumLength(LongitudMaximaDelContenido)
                .WithMessage($"El mensaje admite {LongitudMaximaDelContenido} caracteres como maximo.");
    }
}
