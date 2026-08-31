using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;

namespace Practikap.Application.Validators.Configuracion;

/// <summary>
/// Catalogo cerrado de las claves de configuracion del sistema, con lo que cada una
/// admite como valor y con la descripcion que se persiste al crearla (P8).
/// </summary>
/// <remarks>
/// <b>El catalogo es cerrado a proposito.</b> Un almacen clave/valor abierto dejaria
/// que el panel se llene de entradas que ningun codigo lee: alguien escribe
/// "dias_alerta" donde el sistema espera "dias_sin_seguimiento_alerta", la fila se
/// guarda sin queja, el panel la muestra y nada cambia nunca. Un PUT con una clave
/// que no este aqui responde 422 y <b>no crea la entrada</b>.
///
/// Toma la forma de ReglasDeMotor y de ReglasDeEnumerado, y comparte con ellas el
/// criterio de comparar contra listas explicitas en lugar de Enum.TryParse, que
/// tambien aceptaria la representacion numerica: "1" se convertiria en Pendiente sin
/// que el cliente supiera nunca de esa numeracion, que no vive en ninguna tabla. El
/// contrato de H31 es el nombre exacto (ver <see cref="EsEstadoPractica"/>).
///
/// A diferencia de ReglasDeMotor, aqui <b>no hay extension de FluentValidation</b> y
/// las dos guardas dan 422. La razon es la del DDL, que es la misma que separa a N1
/// y N2 del operador: configuracion.clave y configuracion.valor son VARCHAR libres,
/// la base aceptaria cualquier texto, y lo que los acota es una decision de producto.
/// Lo unico que el validador comprueba es que el valor no venga vacio, que si es
/// forma y da 400 (P8, O19).
///
/// <b>El catalogo crece desde aqui y solo desde aqui.</b> Agregar una clave es
/// agregar una entrada al diccionario y, si su valor no es texto libre, una rama al
/// switch de <see cref="ExigirValorValido"/>. Los mensajes de error se actualizan
/// solos.
///
/// Los mensajes listan los literales admitidos porque son la unica via por la que el
/// panel descubre el catalogo: el GET del listado devuelve las entradas
/// <b>persistidas</b>, y una clave que todavia no se establecio nunca no aparece en
/// ninguna parte hasta que alguien la escribe.
/// </remarks>
public static class ReglasDeConfiguracion
{
    /// <summary>
    /// Clave del estado que se aplica cuando ninguna regla activa del Motor coincide
    /// (RN-06). La consume la Ronda 2.
    /// </summary>
    public const string EstadoPracticaPorDefecto = "estado_practica_por_defecto";

    /// <summary>
    /// Clave de los dias de inactividad a partir de los cuales una practica se
    /// considera en alerta.
    /// </summary>
    public const string DiasSinSeguimientoAlerta = "dias_sin_seguimiento_alerta";

    /// <summary>
    /// El catalogo: clave y la descripcion que se persiste al crear la entrada.
    /// </summary>
    /// <remarks>
    /// La descripcion vive aqui y no la manda el cliente porque describe lo que la
    /// clave significa para el sistema, no lo que el administrador de turno opine de
    /// ella. Por eso tampoco cambia en los PUT posteriores: Configuracion.Establecer
    /// no la toca.
    /// </remarks>
    private static readonly IReadOnlyDictionary<string, string> Catalogo =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [EstadoPracticaPorDefecto] =
                "Estado que se aplica a una practica cuando ninguna regla activa del Motor coincide (RN-06).",

            [DiasSinSeguimientoAlerta] =
                "Dias sin seguimiento a partir de los cuales una practica se considera en alerta."
        };

    /// <summary>Claves admitidas, para el mensaje de error.</summary>
    public static readonly string ClavesAdmitidas = string.Join(", ", Catalogo.Keys);

    /// <summary>
    /// Comprueba que la clave este en el catalogo cerrado de P8.
    /// </summary>
    /// <param name="clave">Clave recibida en la ruta.</param>
    /// <exception cref="ReglaDeDominioException">Si la clave no esta en el catalogo (422).</exception>
    /// <remarks>
    /// La llama el caso de uso, no el validador: el codigo que P8 fija es 422 y el
    /// middleware traduce toda ValidationException a 400. Es el mismo reparto de
    /// ReglasDeMotor.ExigirCampoValido.
    /// </remarks>
    public static void ExigirClaveValida(string clave)
    {
        if (!Catalogo.ContainsKey(clave))
            throw new ReglaDeDominioException(
                $"La clave de configuracion '{clave}' no pertenece al catalogo del sistema. "
                + $"Debe ser una de estas: {ClavesAdmitidas}.",
                "RF-09");
    }

    /// <summary>
    /// Comprueba que el valor sea uno de los que la clave admite.
    /// </summary>
    /// <param name="clave">Clave del catalogo, ya validada por <see cref="ExigirClaveValida"/>.</param>
    /// <param name="valor">Valor recibido en el cuerpo.</param>
    /// <exception cref="ReglaDeDominioException">Si el valor no es de los que la clave admite (422).</exception>
    /// <remarks>
    /// Es lo que impide configurar un valor que nadie sabria interpretar. Sin esta
    /// guarda, un estado_practica_por_defecto con el texto "Activa" se guardaria sin
    /// queja y la Ronda 2 fallaria al leerlo, lejos de aqui y sin nada que apunte a
    /// esta linea: un fallo silencioso en lugar de un rechazo.
    ///
    /// El switch no tiene rama por defecto que rechace: una clave del catalogo cuyo
    /// valor sea texto libre no necesita declarar nada. Hoy las dos declaran algo, y
    /// la asimetria aparecera cuando el catalogo crezca.
    /// </remarks>
    public static void ExigirValorValido(string clave, string valor)
    {
        switch (clave)
        {
            case EstadoPracticaPorDefecto when !EsEstadoPractica(valor):
                throw new ReglaDeDominioException(
                    $"El valor de '{clave}' debe ser uno de estos cuatro: {EstadosAdmitidos}.",
                    "RN-06");

            case DiasSinSeguimientoAlerta when !EsEnteroPositivo(valor):
                throw new ReglaDeDominioException(
                    $"El valor de '{clave}' debe ser un numero entero positivo.",
                    "RF-09");
        }
    }

    /// <summary>Descripcion con la que se crea la entrada de una clave del catalogo.</summary>
    /// <param name="clave">Clave del catalogo, ya validada por <see cref="ExigirClaveValida"/>.</param>
    /// <returns>La descripcion declarada para esa clave.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Si la clave no esta en el catalogo. No deberia ocurrir: el caso de uso llama
    /// antes a <see cref="ExigirClaveValida"/>, y esa es la guarda.
    /// </exception>
    public static string DescripcionDe(string clave) => Catalogo[clave];

    /// <summary>Literales admitidos en estado_practica_por_defecto, para el mensaje de error.</summary>
    private static readonly string EstadosAdmitidos =
        string.Join(", ", Enum.GetNames<EstadoPractica>());

    private static bool EsEstadoPractica(string valor) =>
        Enum.GetNames<EstadoPractica>().Contains(valor, StringComparer.Ordinal);

    /// <summary>
    /// Entero estrictamente positivo. Se admite el signo y los espacios que el
    /// validador ya descarto como vacio, pero no los decimales ni los separadores de
    /// miles: el valor viaja como texto y tiene que poder releerse sin ambiguedad de
    /// cultura.
    /// </summary>
    private static bool EsEnteroPositivo(string valor) =>
        int.TryParse(valor, System.Globalization.NumberStyles.None,
                     System.Globalization.CultureInfo.InvariantCulture, out var numero)
        && numero > 0;
}
