namespace Practikap.Application.DTOs.Reportes;

/// <summary>
/// Un archivo listo para descargar, con todo lo que la respuesta HTTP necesita
/// (RF-08, O24, RN-16).
/// </summary>
/// <remarks>
/// Existe para que el controlador solo haga <c>File(...)</c> y no arme nada: ni el
/// nombre, ni el tipo de contenido, ni los bytes. Es el mismo reparto que sostiene
/// el resto del modulo —el controlador no calcula— llevado al unico endpoint que
/// no devuelve JSON.
///
/// El contenido viaja como <c>byte[]</c> y no como <c>string</c>, y la diferencia
/// no es cosmetica: el BOM que O22 exige es una secuencia de tres bytes, no un
/// caracter, y dejar que ASP.NET Core codificara un string por su cuenta lo
/// perderia. Lo que sale de aqui son los bytes exactos que se escriben en el
/// archivo.
///
/// Es un DTO de salida, no una entidad ni un recurso: nada lo persiste y nadie lo
/// recibe de vuelta. Practikap no guarda los archivos que exporta, porque O14 hace
/// que no haga falta: el contenido se recompone en cada consulta.
/// </remarks>
/// <param name="NombreDeArchivo">Nombre sugerido para la descarga, con su extension.</param>
/// <param name="TipoDeContenido">Tipo MIME con su juego de caracteres.</param>
/// <param name="Contenido">Bytes del archivo, BOM incluido cuando el formato lo lleva.</param>
public sealed record ArchivoExportado
(
    string NombreDeArchivo,
    string TipoDeContenido,
    byte[] Contenido
);
