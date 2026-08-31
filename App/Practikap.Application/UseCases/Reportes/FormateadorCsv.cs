using System.Globalization;
using System.Text;
using Practikap.Application.DTOs.Reportes;
using Practikap.Domain.Enums;

namespace Practikap.Application.UseCases.Reportes;

/// <summary>
/// Traduce un <see cref="ReporteResponse"/> ya compuesto al CSV descargable que
/// cierra RF-08, CU-07 y HU-08b (O22, O24, O25).
/// </summary>
/// <remarks>
/// Es una clase estatica y no un servicio: no tiene estado ni dependencias, asi
/// que no entra en el contenedor y no contradice ADR-05, que enumera casos de uso.
/// Mismo criterio que SerializadorDeFiltro y ArmadorDeReporte.
///
/// <b>No calcula nada.</b> Recibe las lineas y los totales ya compuestos por
/// ArmadorDeReporte y solo los escribe: ni promedia, ni ordena, ni decide que
/// practicas entran. El orden de las filas es el que trae el response, ascendente
/// por identificador de practica, y es lo que hace que el archivo y el JSON digan
/// lo mismo en el mismo orden.
///
/// <b>Sin librerias externas.</b> Ningun .csproj se toca: el formato cabe en un
/// StringBuilder y el entrecomillado, en cuatro lineas. Meter una dependencia de
/// documentos por un entregable secundario era el riesgo que FA-35 difiere.
///
/// Las cuatro decisiones de formato de O22 tienen todas la misma causa —que el
/// archivo abra bien en Excel en Windows, en configuracion regional de Colombia—
/// menos una, que la contradice a proposito:
///
/// <list type="bullet">
/// <item><b>BOM UTF-8.</b> Sin el, Excel lee el archivo en la pagina de codigos
/// del sistema y "Practica" aparece como "PrÃ¡ctica". Es tambien lo unico que
/// permite poner tildes en los encabezados.</item>
/// <item><b>Separador punto y coma.</b> Con coma, Excel en configuracion regional
/// de Colombia —donde la coma es el separador decimal— mete la fila entera en una
/// sola columna.</item>
/// <item><b>Fin de linea CRLF</b> y entrecomillado al estilo RFC 4180, que es lo
/// que cualquier importador espera.</item>
/// <item><b>Decimales con punto.</b> Esta va contra la configuracion regional a
/// sabiendas: Excel los mostrara como texto hasta que el usuario los convierta, y
/// a cambio el archivo sigue siendo legible por cualquier otro consumidor. Un CSV
/// con comas decimales y punto y coma de separador solo lo entiende Excel.</item>
/// </list>
/// </remarks>
internal static class FormateadorCsv
{
    /// <summary>Separador de campos (O22).</summary>
    private const string Separador = ";";

    /// <summary>Fin de linea, explicito y no dependiente del entorno (O22).</summary>
    private const string FinDeLinea = "\r\n";

    /// <summary>Los doce encabezados, en el orden de <see cref="LineaDeReporteResponse"/>.</summary>
    private static readonly string[] Encabezados =
    [
        "Práctica",
        "Aprendiz",
        "Instructor",
        "Ficha",
        "Programa",
        "Empresa",
        "Modalidad",
        "Estado",
        "Fecha de inicio",
        "Fecha de fin",
        "Promedio del instructor",
        "Promedio del aprendiz"
    ];

    /// <summary>
    /// Etiqueta legible de cada estado para el bloque de totales: los literales de
    /// la columna practicas.estado, no los nombres de los miembros.
    /// </summary>
    /// <remarks>
    /// <b>Duplica cuatro literales que ConvertidoresDeEnum ya declara</b>, y la
    /// duplicacion es deliberada: ese convertidor vive en la Infraestructura y la
    /// Aplicacion no la referencia. Traerla para cuatro cadenas invertiria la
    /// dependencia de la arquitectura por una etiqueta.
    ///
    /// De aqui sale la unica inconsistencia interna del archivo, y esta buscada: la
    /// columna Estado de cada linea sale como "EnCurso" —es el valor que el JSON
    /// informa, y O23 existe para que las dos salidas no diverjan— mientras que la
    /// etiqueta del total sale como "En curso". La primera es un dato y tiene que
    /// coincidir con la API; la segunda es un rotulo y se lee.
    ///
    /// El recorrido se hace sobre Enum.GetNames y no sobre las claves de este
    /// diccionario, de modo que un quinto estado apareceria igual en el archivo,
    /// rotulado con su nombre de miembro hasta que alguien agregue su literal.
    /// </remarks>
    private static readonly Dictionary<string, string> EtiquetasDeEstado = new(StringComparer.Ordinal)
    {
        [nameof(EstadoPractica.Pendiente)] = "Pendiente",
        [nameof(EstadoPractica.EnCurso)] = "En curso",
        [nameof(EstadoPractica.Finalizada)] = "Finalizada",
        [nameof(EstadoPractica.EnRiesgo)] = "En riesgo"
    };

    /// <summary>Formatea un reporte compuesto como archivo CSV descargable.</summary>
    /// <param name="reporte">Reporte con su rastro, sus lineas y sus totales ya compuestos.</param>
    /// <returns>El archivo, con su nombre, su tipo de contenido y sus bytes.</returns>
    /// <remarks>
    /// La estructura es cabecera, una fila por linea, <b>una fila en blanco</b> y el
    /// bloque de totales (O25). El blanco y el orden no son estetica: un importador
    /// que corte en la primera fila vacia —que es lo que hacen casi todos— sigue
    /// obteniendo una tabla valida de doce columnas. Poner los totales arriba, o
    /// pegados a los datos, habria roto esa lectura.
    ///
    /// El nombre lleva la fecha de <b>la exportacion</b> y no la de generacion del
    /// reporte, por coherencia con O14: lo que el archivo contiene son los datos de
    /// hoy, aunque el reporte se haya generado hace un mes.
    /// </remarks>
    public static ArchivoExportado Formatear(ReporteResponse reporte)
    {
        var contenido = new StringBuilder();

        EscribirFila(contenido, Encabezados);

        foreach (var linea in reporte.Lineas)
            EscribirFila(contenido, Celdas(linea));

        // La fila en blanco que separa la tabla del bloque de totales (O25).
        contenido.Append(FinDeLinea);

        EscribirTotales(contenido, reporte.Totales);

        return new ArchivoExportado(
            $"reporte_{reporte.Id}_{DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.csv",
            "text/csv; charset=utf-8",
            // Encoding.UTF8.GetBytes no emite el BOM por su cuenta: hay que
            // anteponer el preambulo a mano (O22).
            [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(contenido.ToString())]);
    }

    /// <summary>Proyecta una linea del reporte a sus doce celdas, en orden.</summary>
    /// <remarks>
    /// La empresa nula sale como celda vacia y nunca como la palabra "null": es
    /// nula en ProyectoProductivo y en Monitoria (H22, H25), que son modalidades
    /// normales y no datos faltantes. La fecha de fin sigue el mismo criterio
    /// mientras la practica no se haya finalizado.
    /// </remarks>
    private static string[] Celdas(LineaDeReporteResponse linea) =>
    [
        linea.PracticaId.ToString(CultureInfo.InvariantCulture),
        linea.Aprendiz,
        linea.Instructor,
        linea.Ficha,
        linea.Programa,
        linea.Empresa ?? string.Empty,
        linea.Modalidad,
        linea.Estado,
        Fecha(linea.FechaInicio),
        linea.FechaFin is null ? string.Empty : Fecha(linea.FechaFin.Value),
        Decimal(linea.PromedioInstructor),
        Decimal(linea.PromedioAprendiz)
    ];

    /// <summary>Escribe el bloque de totales, vertical y de dos columnas.</summary>
    /// <remarks>
    /// Vertical y no en una fila de seis columnas para que cada numero llegue con
    /// su rotulo al lado y no haya que contar columnas para saber cual es cual. Es
    /// ademas la forma que sobrevive a que el bloque gane un total mas: agrega una
    /// fila en lugar de desplazar columnas.
    /// </remarks>
    private static void EscribirTotales(StringBuilder contenido, TotalesDeReporteResponse totales)
    {
        EscribirFila(contenido, [
            "Cantidad de prácticas",
            totales.CantidadDePracticas.ToString(CultureInfo.InvariantCulture)]);

        foreach (var miembro in Enum.GetNames<EstadoPractica>())
        {
            var etiqueta = EtiquetasDeEstado.TryGetValue(miembro, out var literal) ? literal : miembro;
            var cantidad = totales.DistribucionPorEstado.TryGetValue(miembro, out var valor) ? valor : 0;

            EscribirFila(contenido, [etiqueta, cantidad.ToString(CultureInfo.InvariantCulture)]);
        }

        EscribirFila(contenido, ["Promedio general", Decimal(totales.PromedioGeneral)]);
    }

    /// <summary>Escribe una fila con sus celdas escapadas y su fin de linea.</summary>
    private static void EscribirFila(StringBuilder contenido, string[] celdas) =>
        contenido.Append(string.Join(Separador, celdas.Select(Escapar))).Append(FinDeLinea);

    /// <summary>
    /// Entrecomilla una celda al estilo RFC 4180, solo cuando hace falta.
    /// </summary>
    /// <remarks>
    /// Entrecomillar todo habria sido mas simple y tambien valido, pero deja un
    /// archivo ilegible al abrirlo en un editor de texto. Se entrecomilla lo que lo
    /// necesita —el separador, la comilla y el salto de linea— y las comillas
    /// internas se duplican, que es como la norma las escapa.
    /// </remarks>
    private static string Escapar(string celda) =>
        celda.Contains(Separador, StringComparison.Ordinal)
        || celda.Contains('"')
        || celda.Contains('\r')
        || celda.Contains('\n')
            ? $"\"{celda.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : celda;

    /// <summary>Fecha en formato ISO, que ningun consumidor interpreta al reves.</summary>
    private static string Fecha(DateOnly fecha) =>
        fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Decimal con dos cifras y punto separador (O22).</summary>
    private static string Decimal(decimal valor) =>
        valor.ToString("0.00", CultureInfo.InvariantCulture);
}
