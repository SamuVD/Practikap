using FluentValidation;
using Practikap.Domain.Enums;

namespace Practikap.Application.Validators.Reportes;

/// <summary>
/// Reglas compartidas del enumerado que M7 expone como texto (H31).
/// </summary>
/// <remarks>
/// Se comparan los literales contra Enum.GetNames y no con Enum.TryParse, que
/// tambien aceptaria la representacion numerica: "1" se convertiria en Individual
/// sin que el cliente supiera nunca de esa numeracion, que no vive en ninguna
/// tabla. El contrato de H31 es el nombre exacto, y esta comparacion es lo unico
/// que lo hace cierto. Es el mismo criterio de ReglasDeEnumerado y ReglasDeMotor.
///
/// El tipo da <b>400 y no 422</b>, a diferencia del campo evaluado y la accion
/// resultante de N1 y N2. La diferencia no es de gusto sino del DDL: reportes.tipo
/// es enum('Individual','Grupal'), un enumerado cerrado igual que Modalidad,
/// Estado y Operador, y un literal fuera de esa lista es un error de forma que el
/// validador puede rechazar sin consultar nada. Los de N1 y N2 eran VARCHAR
/// libres, que es por lo que aquellos se comprueban en el caso de uso.
///
/// Estado y Modalidad del filtro no se validan aqui aunque tambien sean
/// enumerados cerrados. O19 los manda al caso de uso con 422, con el mismo
/// criterio y las mismas palabras con que ListarPracticasUseCase trata su
/// parametro estado: un filtro con un literal desconocido no es una peticion mal
/// formada sino una que el sistema no puede procesar. Ademas son opcionales, y
/// las dos extensiones de ReglasDeEnumerado exigen NotEmpty.
/// </remarks>
public static class ReglasDeReporte
{
    /// <summary>Literales admitidos en el campo Tipo, para el mensaje de error.</summary>
    public static readonly string TiposAdmitidos =
        string.Join(", ", Enum.GetNames<TipoReporte>());

    /// <summary>Exige que el valor sea uno de los nombres de <see cref="TipoReporte"/>.</summary>
    /// <typeparam name="T">DTO que declara la propiedad.</typeparam>
    /// <param name="regla">Constructor de reglas sobre la propiedad.</param>
    /// <returns>La regla encadenable.</returns>
    public static IRuleBuilderOptions<T, string> ConTipoValido<T>(
        this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("El tipo de reporte es obligatorio.")
            .Must(EsTipo)
                .WithMessage($"El tipo debe ser uno de estos dos: {TiposAdmitidos}.");

    private static bool EsTipo(string? valor) =>
        valor is not null && Enum.GetNames<TipoReporte>().Contains(valor, StringComparer.Ordinal);
}
