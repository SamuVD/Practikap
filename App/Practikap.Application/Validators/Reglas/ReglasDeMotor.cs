using FluentValidation;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;

namespace Practikap.Application.Validators.Reglas;

/// <summary>
/// Listas blancas de los tres campos de texto cerrado que una regla del Motor
/// declara: el operador, el campo evaluado y la accion resultante (N1, N2).
/// </summary>
/// <remarks>
/// Los literales se comparan contra listas explicitas y no con Enum.TryParse, que
/// tambien aceptaria la representacion numerica: "1" se convertiria en Mayor sin
/// que el cliente supiera nunca de esa numeracion, que no vive en ninguna tabla.
/// El contrato de H31 es el nombre exacto, y esta comparacion es lo unico que lo
/// hace cierto. Es el mismo criterio de ReglasDeEnumerado, del que este archivo
/// toma la forma.
///
/// <b>Los tres no dan el mismo codigo, y la diferencia es deliberada.</b> El
/// operador se valida como regla de FluentValidation y da 400: es un enumerado
/// cerrado por el DDL —la columna es ENUM('&gt;','&gt;=','&lt;','&lt;=','=','!=')—,
/// exactamente igual que Modalidad y Estado en M3, y un valor fuera de el es un
/// error de forma. El campo evaluado y la accion resultante, en cambio, son
/// VARCHAR libres en el DDL: la base aceptaria cualquier texto y lo que los acota
/// es una decision de producto (N1, N2), no la forma del mensaje. Por eso se
/// comprueban en el caso de uso y dan 422, que es lo que esas decisiones fijan.
///
/// La consecuencia practica es que este archivo expone dos cosas distintas: una
/// extension de FluentValidation para el operador, y dos guardas que lanzan
/// ReglaDeDominioException para los otros dos.
///
/// <b>Las dos listas crecen desde aqui y solo desde aqui.</b> Cuando un paso
/// posterior agregue un campo evaluable, se agrega el literal a un unico arreglo
/// de este archivo, el mensaje de error se actualiza solo y el evaluador de la
/// Ronda 2 lo recorre sin cambios. Lo unico que hay que agregar en paralelo es la
/// medicion del campo nuevo en <c>EvaluadorDeReglas.ValorObservadoAsync</c>.
/// </remarks>
public static class ReglasDeMotor
{
    /// <summary>
    /// Campos del dominio que una regla puede evaluar (N1). El primero es el que
    /// alimenta RN-09 desde las calificaciones; el segundo, la inactividad de
    /// seguimiento que M4 registra.
    /// </summary>
    /// <remarks>
    /// Es publica, a diferencia de <see cref="Acciones"/>, porque el evaluador la
    /// recorre para saber que medir (N13). Sin ella tendria que repetir los dos
    /// literales, y la promesa de que las listas crecen desde este archivo y solo
    /// desde el dejaria de ser cierta. Se expone como IReadOnlyList y no como
    /// arreglo para que nadie pueda reescribirla desde fuera.
    ///
    /// El orden importa poco: el desempate entre campos lo resuelve la prioridad de
    /// cada regla, no la posicion del campo (RN-07).
    /// </remarks>
    public static readonly IReadOnlyList<string> Campos =
    [
        "calificacion_acumulada",
        "dias_sin_seguimiento"
    ];

    /// <summary>Consecuencias que una regla puede producir al coincidir (N2).</summary>
    private static readonly string[] Acciones =
    [
        "MarcarEnRiesgo",
        "NotificarInstructor",
        "MarcarEnRiesgoYNotificar"
    ];

    /// <summary>Literales admitidos en el campo CampoEvaluado, para el mensaje de error.</summary>
    public static readonly string CamposAdmitidos = string.Join(", ", Campos);

    /// <summary>Literales admitidos en el campo AccionResultante, para el mensaje de error.</summary>
    public static readonly string AccionesAdmitidas = string.Join(", ", Acciones);

    /// <summary>Literales admitidos en el campo Operador, para el mensaje de error.</summary>
    public static readonly string OperadoresAdmitidos =
        string.Join(", ", Enum.GetNames<OperadorComparacion>());

    /// <summary>
    /// Exige que el valor sea uno de los nombres de
    /// <see cref="OperadorComparacion"/>. Un valor fuera de la lista es un 400.
    /// </summary>
    /// <typeparam name="T">DTO que declara la propiedad.</typeparam>
    /// <param name="regla">Constructor de reglas sobre la propiedad.</param>
    /// <returns>La regla encadenable.</returns>
    public static IRuleBuilderOptions<T, string> ConOperadorValido<T>(
        this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("El operador es obligatorio.")
            .Must(EsOperador)
                .WithMessage($"El operador debe ser uno de estos: {OperadoresAdmitidos}.");

    /// <summary>
    /// Comprueba que el campo evaluado este en la lista blanca de N1.
    /// </summary>
    /// <param name="campoEvaluado">Literal recibido en el DTO.</param>
    /// <exception cref="ReglaDeDominioException">
    /// Si el campo no es uno de los admitidos (422).
    /// </exception>
    /// <remarks>
    /// La llaman los casos de uso de alta y de edicion, no los validadores: el
    /// codigo que N1 fija es 422 y el middleware traduce toda ValidationException
    /// a 400.
    ///
    /// Es lo que impide configurar una regla sobre un campo que el Motor no sabria
    /// medir. Sin esta guarda la regla se guardaria sin queja y luego jamas
    /// coincidiria con nada, porque MotorDeReglas.Evaluar descarta las reglas cuyo
    /// campo no es el consultado: un fallo silencioso en lugar de un rechazo.
    /// </remarks>
    public static void ExigirCampoValido(string campoEvaluado)
    {
        if (!Campos.Contains(campoEvaluado, StringComparer.Ordinal))
            throw new ReglaDeDominioException(
                $"El campo evaluado '{campoEvaluado}' no es evaluable por el Motor. "
                + $"Debe ser uno de estos: {CamposAdmitidos}.",
                "RF-10");
    }

    /// <summary>
    /// Comprueba que la accion resultante este en la lista blanca de N2.
    /// </summary>
    /// <param name="accionResultante">Literal recibido en el DTO.</param>
    /// <exception cref="ReglaDeDominioException">
    /// Si la accion no es una de las admitidas (422).
    /// </exception>
    /// <remarks>
    /// La llaman los casos de uso, por la misma razon de codigo que
    /// <see cref="ExigirCampoValido"/>.
    ///
    /// Es lo que impide configurar una consecuencia que nadie sabria ejecutar. Los
    /// tres literales son los que el cableado de la Ronda 2 reconoce en
    /// <c>EvaluadorDeReglas.AplicarAsync</c>: marcar la practica en riesgo (RN-09),
    /// notificar al instructor (RF-07, RN-09) o las dos cosas.
    /// </remarks>
    public static void ExigirAccionValida(string accionResultante)
    {
        if (!Acciones.Contains(accionResultante, StringComparer.Ordinal))
            throw new ReglaDeDominioException(
                $"La accion resultante '{accionResultante}' no es ejecutable por el Motor. "
                + $"Debe ser una de estas: {AccionesAdmitidas}.",
                "RF-10");
    }

    private static bool EsOperador(string? valor) =>
        valor is not null
        && Enum.GetNames<OperadorComparacion>().Contains(valor, StringComparer.Ordinal);
}
