using FluentValidation;
using Practikap.Application.DTOs.Notificaciones;

namespace Practikap.Application.Validators.Notificaciones;

/// <summary>Validacion de forma de <see cref="CrearNotificacionRequest"/> (RN-15).</summary>
/// <remarks>
/// El contenido vacio se comprueba aqui y no solo en el Dominio porque el
/// endpoint debe responder 400 para ese caso, y una ReglaDeDominioException se
/// traduce a 422. La guarda del constructor de Notificacion queda como respaldo
/// del camino que no pasa por este validador, que es el de los tres eventos de L5.
///
/// Aqui solo se descarta lo imposible por forma. Que el destinatario exista se
/// comprueba en el caso de uso, contra IUsuarioRepository.
///
/// No usa ReglasDeEnumerado: el tipo no viaja en la entrada, lo fija el caso de
/// uso en Administrativa (L2).
/// </remarks>
public sealed class CrearNotificacionRequestValidator : AbstractValidator<CrearNotificacionRequest>
{
    /// <summary>
    /// Tope de longitud del contenido. A diferencia del tope de K10 en la
    /// mensajeria, que era una cota de producto sobre una columna TEXT, este es el
    /// limite fisico: notificaciones.contenido es VARCHAR(255) en el
    /// Script_DDL.sql. Declararlo aqui convierte en un 400 legible lo que si no
    /// seria un error de truncamiento de MySQL.
    /// </summary>
    private const int LongitudMaximaDelContenido = 255;

    /// <summary>Declara las reglas de validacion.</summary>
    public CrearNotificacionRequestValidator()
    {
        RuleFor(peticion => peticion.UsuarioId)
            .GreaterThan(0).WithMessage("Debe indicar el usuario destinatario de la notificacion.");

        RuleFor(peticion => peticion.Contenido)
            .NotEmpty().WithMessage("El contenido de la notificacion es obligatorio.")
            .MaximumLength(LongitudMaximaDelContenido)
                .WithMessage($"La notificacion admite {LongitudMaximaDelContenido} caracteres como maximo.");
    }
}
