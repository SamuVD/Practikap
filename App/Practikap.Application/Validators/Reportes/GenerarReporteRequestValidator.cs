using FluentValidation;
using Practikap.Application.DTOs.Reportes;

namespace Practikap.Application.Validators.Reportes;

/// <summary>
/// Validacion de forma de la generacion de un reporte (O7, O19).
/// </summary>
/// <remarks>
/// Dos reglas y nada mas, porque solo hay dos cosas que se puedan decidir sin
/// consultar la base ni el token.
///
/// El tipo es un enumerado cerrado por el DDL y su literal se comprueba aqui, con
/// 400. El rango de fechas invertido tambien: que Hasta preceda a Desde es un
/// error de forma que se ve mirando el cuerpo, y ninguna consulta lo volveria
/// valido. Es el mismo criterio con que CrearPracticaRequestValidator trata su
/// propio rango.
///
/// Lo que no esta aqui esta ausente a proposito. Los identificadores del filtro no
/// se comprueban contra la base: un instructorId inexistente no es una peticion
/// mal formada, es un filtro que no selecciona nada, y O8 ya dice que eso responde
/// 200 con contenido vacio. Comprobarlos costaria cinco consultas para convertir
/// en 400 lo que el sistema resuelve mejor como lista vacia, y ademas filtraria la
/// existencia de recursos fuera del alcance del solicitante, que es lo contrario
/// de lo que RN-13 persigue (O13).
///
/// La comparacion del rango se hace sobre el request y no sobre
/// FiltroReporte.RangoEsValido, que dice lo mismo: aquel metodo opera sobre el
/// objeto de valor, que solo existe despues de la traduccion que hace el caso de
/// uso, y para entonces el 400 ya tiene que haber salido.
/// </remarks>
public sealed class GenerarReporteRequestValidator : AbstractValidator<GenerarReporteRequest>
{
    /// <summary>Declara las reglas de validacion del DTO.</summary>
    public GenerarReporteRequestValidator()
    {
        RuleFor(peticion => peticion.Tipo).ConTipoValido();

        RuleFor(peticion => peticion.Filtro)
            .Must(RangoEsValido)
                .WithMessage("La fecha Hasta del filtro no puede ser anterior a la fecha Desde.");
    }

    /// <summary>
    /// Comprueba que el rango de fechas de inicio no este invertido. Un filtro
    /// ausente o con alguno de los dos limites sin establecer siempre es valido:
    /// los nueve criterios son opcionales.
    /// </summary>
    private static bool RangoEsValido(FiltroReporteRequest? filtro) =>
        filtro is null
        || filtro.Desde is null
        || filtro.Hasta is null
        || filtro.Hasta >= filtro.Desde;
}
