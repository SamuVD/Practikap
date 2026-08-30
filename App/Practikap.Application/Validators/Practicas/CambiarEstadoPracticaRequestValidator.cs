using FluentValidation;
using Practikap.Application.DTOs.Practicas;

namespace Practikap.Application.Validators.Practicas;

/// <summary>Validacion de forma de <see cref="CambiarEstadoPracticaRequest"/> (RN-15).</summary>
/// <remarks>
/// Que la transicion sea legal segun RN-05 no se valida aqui: eso depende del
/// estado actual de la practica, que este DTO no conoce. Lo decide
/// Practica.CambiarEstado, invocada desde el caso de uso (H28).
///
/// Que FechaFin no preceda a FechaInicio tampoco: la fecha de inicio vive en la
/// entidad. Lo comprueba Practica.Finalizar (H30).
/// </remarks>
public sealed class CambiarEstadoPracticaRequestValidator
    : AbstractValidator<CambiarEstadoPracticaRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public CambiarEstadoPracticaRequestValidator()
    {
        RuleFor(peticion => peticion.Estado).ConEstadoValido();
    }
}
