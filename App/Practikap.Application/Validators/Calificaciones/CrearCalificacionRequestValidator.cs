using FluentValidation;
using Practikap.Application.DTOs.Calificaciones;
using Practikap.Domain.Entities;

namespace Practikap.Application.Validators.Calificaciones;

/// <summary>Validacion de forma de <see cref="CrearCalificacionRequest"/> (RN-15).</summary>
/// <remarks>
/// Un solo validador para los dos POST, porque hay un solo DTO de entrada.
///
/// El rango se comprueba aqui y no solo en el Dominio porque CU-05 y HU-06 piden
/// 400 para el valor fuera de rango, y una ReglaDeDominioException se traduce a
/// 422. La guarda de CalificacionInstructor.Anular y su gemela quedan como
/// respaldo del camino que no pasa por este validador, junto con las
/// restricciones chk_calificaciones_* de la base: tres barreras independientes
/// para la misma regla.
///
/// Aqui solo se descartan valores imposibles por forma. Que la practica exista,
/// que este En curso o En riesgo (J4) y que el solicitante sea el instructor o el
/// aprendiz de esa practica se comprueba en el caso de uso, contra el repositorio
/// y contra IContextoUsuario.
///
/// No usa ReglasDeEnumerado: M5 no expone ningun enumerado.
/// </remarks>
public sealed class CrearCalificacionRequestValidator : AbstractValidator<CrearCalificacionRequest>
{
    /// <summary>Decimales que admite la columna valor, DECIMAL(3,1) en el Script_DDL.sql.</summary>
    private const int DecimalesAdmitidos = 1;

    /// <summary>Declara las reglas de validacion.</summary>
    public CrearCalificacionRequestValidator()
    {
        RuleFor(peticion => peticion.PracticaId)
            .GreaterThan(0).WithMessage("Debe indicar la practica que se califica.");

        // Los limites salen de las constantes de la entidad, que a su vez
        // replican el CHECK del Script_DDL.sql. Escribir 0.0 y 5.0 a mano aqui
        // habria abierto la puerta a que las tres barreras dejaran de coincidir.
        RuleFor(peticion => peticion.Valor)
            .InclusiveBetween(CalificacionInstructor.ValorMinimo, CalificacionInstructor.ValorMaximo)
                .WithMessage(
                    $"La calificacion debe estar entre {CalificacionInstructor.ValorMinimo:0.0} " +
                    $"y {CalificacionInstructor.ValorMaximo:0.0}.")
            // Sin esta regla un 4.55 entraria y MySQL lo guardaria redondeado en
            // silencio, devolviendo despues un valor que el cliente nunca envio.
            .Must(TieneUnDecimalComoMucho)
                .WithMessage("La calificacion admite un solo decimal.");

        // Comentario es TEXT NULL: no hay tope de forma que imponer, y tampoco
        // presencia que exigir. El recorte y el null-si-viene-en-blanco los hace
        // el constructor de la entidad.
    }

    /// <summary>
    /// Comprueba que el valor no traiga mas decimales de los que la columna
    /// puede almacenar.
    /// </summary>
    /// <param name="valor">Valor recibido.</param>
    /// <returns>true si tiene un decimal o ninguno.</returns>
    /// <remarks>
    /// La escala vive en el cuarto entero de decimal.GetBits, que es donde el
    /// tipo guarda cuantos decimales declaro el literal. Un 4.50 declara dos y se
    /// normaliza antes de compararlo, para no rechazar un valor que la columna si
    /// puede representar.
    /// </remarks>
    private static bool TieneUnDecimalComoMucho(decimal valor)
    {
        var normalizado = valor / 1.000000000000000000000000000000000m;
        var escala = (decimal.GetBits(normalizado)[3] >> 16) & 0xFF;

        return escala <= DecimalesAdmitidos;
    }
}
