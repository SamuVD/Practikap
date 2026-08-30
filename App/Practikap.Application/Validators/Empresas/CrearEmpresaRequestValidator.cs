using FluentValidation;
using Practikap.Application.DTOs.Empresas;

namespace Practikap.Application.Validators.Empresas;

/// <summary>Validacion de forma de <see cref="CrearEmpresaRequest"/> (RN-15).</summary>
/// <remarks>
/// Que el NIT no este repetido se comprueba en el caso de uso contra
/// uq_empresas_nit: aqui solo se valida la forma.
/// </remarks>
public sealed class CrearEmpresaRequestValidator : AbstractValidator<CrearEmpresaRequest>
{
    /// <summary>Declara las reglas de validacion.</summary>
    public CrearEmpresaRequestValidator()
    {
        RuleFor(peticion => peticion.RazonSocial)
            .NotEmpty().WithMessage("La razon social es obligatoria.")
            .MaximumLength(200).WithMessage("La razon social no puede superar 200 caracteres.");

        RuleFor(peticion => peticion.Nit)
            .NotEmpty().WithMessage("El NIT es obligatorio.")
            .MaximumLength(20).WithMessage("El NIT no puede superar 20 caracteres.");

        RuleFor(peticion => peticion.JefeInmediatoNombre)
            .MaximumLength(150).WithMessage("El nombre del jefe inmediato no puede superar 150 caracteres.")
            .When(peticion => !string.IsNullOrWhiteSpace(peticion.JefeInmediatoNombre));

        RuleFor(peticion => peticion.JefeInmediatoCorreo)
            .EmailAddress().WithMessage("El correo del jefe inmediato no tiene un formato valido.")
            .MaximumLength(180).WithMessage("El correo del jefe inmediato no puede superar 180 caracteres.")
            .When(peticion => !string.IsNullOrWhiteSpace(peticion.JefeInmediatoCorreo));

        RuleFor(peticion => peticion.JefeInmediatoTelefono)
            .MaximumLength(20).WithMessage("El telefono del jefe inmediato no puede superar 20 caracteres.")
            .When(peticion => !string.IsNullOrWhiteSpace(peticion.JefeInmediatoTelefono));
    }
}
