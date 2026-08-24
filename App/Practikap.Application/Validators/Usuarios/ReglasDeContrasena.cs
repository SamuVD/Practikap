using FluentValidation;

namespace Practikap.Application.Validators.Usuarios;

/// <summary>
/// Politica unica de contrasenas (decision F5). Se declara una sola vez y la
/// reutilizan los tres validadores que aceptan una contrasena nueva, para que
/// no puedan divergir entre si.
/// </summary>
public static class ReglasDeContrasena
{
    /// <summary>Longitud minima aceptada.</summary>
    public const int LongitudMinima = 8;

    /// <summary>
    /// Longitud maxima aceptada. No es una cifra arbitraria: BCrypt ignora todo
    /// lo que exceda 72 bytes, de modo que sin este tope dos contrasenas
    /// distintas que compartan los primeros 72 caracteres abririan la misma
    /// cuenta.
    /// </summary>
    public const int LongitudMaxima = 72;

    /// <summary>Aplica la politica de contrasenas a una regla existente.</summary>
    /// <typeparam name="T">Tipo del DTO que se valida.</typeparam>
    /// <param name="regla">Regla sobre la propiedad de contrasena.</param>
    /// <returns>La misma regla, para permitir encadenamiento.</returns>
    public static IRuleBuilderOptions<T, string> ConPoliticaDeContrasena<T>(
        this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("La contrasena es obligatoria.")
            .MinimumLength(LongitudMinima)
                .WithMessage($"La contrasena debe tener al menos {LongitudMinima} caracteres.")
            .MaximumLength(LongitudMaxima)
                .WithMessage($"La contrasena no puede superar {LongitudMaxima} caracteres.")
            .Matches("[A-Za-zÁÉÍÓÚáéíóúÑñ]").WithMessage("La contrasena debe incluir al menos una letra.")
            .Matches("[0-9]").WithMessage("La contrasena debe incluir al menos un digito.");
}