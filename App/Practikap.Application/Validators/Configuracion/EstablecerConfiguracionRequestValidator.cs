using FluentValidation;
using Practikap.Application.DTOs.Configuracion;

namespace Practikap.Application.Validators.Configuracion;

/// <summary>
/// Validacion de forma del establecimiento de una clave de configuracion (P8, O19).
/// </summary>
/// <remarks>
/// Una sola regla, porque solo hay una cosa que se pueda decidir sin conocer la
/// clave: que el valor venga. Un cuerpo con el valor vacio es un error de forma que
/// se ve mirando el cuerpo, y ninguna consulta lo volveria valido.
///
/// Lo que no esta aqui esta ausente a proposito. Ni la clave, que viaja en la ruta y
/// no en el cuerpo, ni lo que esa clave admite como valor: el catalogo de P8 es una
/// decision de producto, no la forma del mensaje, y por eso lo comprueba el caso de
/// uso con 422. Es el mismo reparto con que N1 y N2 mandaron el campo evaluado y la
/// accion resultante fuera del validador de reglas.
///
/// Tampoco se comprueba la longitud contra los 255 de configuracion.valor. Un valor
/// que la exceda no es una peticion mal formada sino una que el catalogo ya habria
/// rechazado: ninguna de las dos claves de P8 admite un texto largo, y la unica que
/// podria hacerlo el dia que exista lo declarara en ReglasDeConfiguracion, que es
/// donde vive lo que cada clave admite.
/// </remarks>
public sealed class EstablecerConfiguracionRequestValidator
    : AbstractValidator<EstablecerConfiguracionRequest>
{
    /// <summary>Declara las reglas de validacion del DTO.</summary>
    public EstablecerConfiguracionRequestValidator()
    {
        RuleFor(peticion => peticion.Valor)
            .NotEmpty().WithMessage("El valor de configuracion es obligatorio.");
    }
}
