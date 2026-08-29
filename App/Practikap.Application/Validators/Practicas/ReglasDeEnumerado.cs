using FluentValidation;
using Practikap.Domain.Enums;

namespace Practikap.Application.Validators.Practicas;

/// <summary>
/// Reglas compartidas de los dos enumerados que M3 expone como texto (H31).
/// </summary>
/// <remarks>
/// Se comparan los literales contra Enum.GetNames y no con Enum.TryParse, que
/// tambien aceptaria la representacion numerica: "1" se convertiria en
/// ContratoDeAprendizaje sin que el cliente supiera nunca de esa numeracion, que
/// no vive en ninguna tabla. El contrato de H31 es el nombre exacto, y esta
/// comparacion es lo unico que lo hace cierto.
/// </remarks>
public static class ReglasDeEnumerado
{
    /// <summary>Literales admitidos en el campo Modalidad, para el mensaje de error.</summary>
    public static readonly string ModalidadesAdmitidas =
        string.Join(", ", Enum.GetNames<ModalidadPractica>());

    /// <summary>Literales admitidos en el campo Estado, para el mensaje de error.</summary>
    public static readonly string EstadosAdmitidos =
        string.Join(", ", Enum.GetNames<EstadoPractica>());

    /// <summary>Exige que el valor sea uno de los nombres de <see cref="ModalidadPractica"/>.</summary>
    /// <typeparam name="T">DTO que declara la propiedad.</typeparam>
    /// <param name="regla">Constructor de reglas sobre la propiedad.</param>
    /// <returns>La regla encadenable.</returns>
    public static IRuleBuilderOptions<T, string> ConModalidadValida<T>(
        this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("La modalidad es obligatoria.")
            .Must(EsModalidad)
                .WithMessage($"La modalidad debe ser una de estas cuatro: {ModalidadesAdmitidas}.");

    /// <summary>Exige que el valor sea uno de los nombres de <see cref="EstadoPractica"/>.</summary>
    /// <typeparam name="T">DTO que declara la propiedad.</typeparam>
    /// <param name="regla">Constructor de reglas sobre la propiedad.</param>
    /// <returns>La regla encadenable.</returns>
    public static IRuleBuilderOptions<T, string> ConEstadoValido<T>(
        this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("El estado es obligatorio.")
            .Must(EsEstado)
                .WithMessage($"El estado debe ser uno de estos cuatro: {EstadosAdmitidos}.");

    private static bool EsModalidad(string? valor) =>
        valor is not null && Enum.GetNames<ModalidadPractica>().Contains(valor, StringComparer.Ordinal);

    private static bool EsEstado(string? valor) =>
        valor is not null && Enum.GetNames<EstadoPractica>().Contains(valor, StringComparer.Ordinal);
}
