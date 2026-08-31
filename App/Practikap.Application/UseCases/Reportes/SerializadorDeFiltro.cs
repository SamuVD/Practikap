using System.Text.Json;
using System.Text.Json.Serialization;
using Practikap.Domain.ValueObjects;

namespace Practikap.Application.UseCases.Reportes;

/// <summary>
/// Traduce un <see cref="FiltroReporte"/> al JSON que guarda la columna
/// reportes.filtros (O11).
/// </summary>
/// <remarks>
/// Es una clase estatica y no un servicio: no tiene estado ni dependencias, asi
/// que no entra en el contenedor y no contradice ADR-05, que enumera casos de
/// uso. Mismo criterio que ParticipantesDePractica y AccesoALaPractica.
///
/// La serializacion vive aqui y no en la Infraestructura porque asi lo reparte el
/// Dominio: FiltroReporte documenta que la forma del criterio es suya y la
/// serializacion es de Aplicacion, y ReporteConfiguration mapea la columna como
/// json <b>sin convertidor</b> precisamente para no duplicar esa decision.
///
/// Las tres opciones no son cosmeticas. camelCase deja el JSON persistido con la
/// misma forma que el JSON de la API, de modo que el rastro se lee igual que se
/// escribio. Los nulos se omiten porque un filtro guarda lo que se pidio, no los
/// ocho criterios que no se usaron: un filtro por ficha se lee de un vistazo en
/// lugar de esconderse entre nulos. Y los enumerados salen como texto (H31), que
/// es lo unico que hace el rastro legible: guardar 2 obligaria a conocer una
/// numeracion que no vive en ninguna tabla para saber que se filtro por Pasantia.
///
/// El proyecto no registra JsonStringEnumConverter globalmente en Program.cs
/// —todos los DTO exponen sus enumerados como string y los perfiles los proyectan
/// con ToString—, de modo que aqui hace falta declararlo. Estas opciones no
/// afectan a la serializacion de las respuestas HTTP: son de este serializador y
/// de nadie mas.
///
/// Un filtro sin ningun criterio produce "{}", que no es blanco y por tanto pasa
/// la guarda del constructor de Reporte. Es lo correcto: el reporte de todo el
/// alcance del solicitante tambien tiene un rastro que dejar.
/// </remarks>
internal static class SerializadorDeFiltro
{
    /// <summary>
    /// Opciones unicas y de solo lectura. Una sola instancia estatica porque
    /// JsonSerializerOptions cachea metadatos de contrato en su primer uso:
    /// construir una por llamada rehace ese trabajo en cada generacion de reporte.
    /// </summary>
    private static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Serializa el filtro al JSON que se persiste como rastro.</summary>
    /// <param name="filtro">Criterios aplicados.</param>
    /// <returns>El JSON del filtro. "{}" si no impone ningun criterio.</returns>
    public static string Serializar(FiltroReporte filtro) =>
        JsonSerializer.Serialize(filtro, Opciones);
}
