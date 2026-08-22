namespace Practikap.Application.Common;

/// <summary>
/// Forma unica de toda respuesta de error del sistema, segun Doc_Tecnico 5.9.
/// Serializada con la politica web por defecto de ASP.NET Core produce
/// exactamente las claves codigo, mensaje, detalles y traza.
/// </summary>
/// <param name="Codigo">Codigo HTTP de la respuesta.</param>
/// <param name="Mensaje">Texto apto para mostrar al usuario final.</param>
/// <param name="Detalles">Un elemento por campo en los errores de validacion; vacio en el resto.</param>
/// <param name="Traza">
/// Identificador unico del error. Se devuelve al cliente y se escribe en el
/// registro del servidor con el mismo valor, para poder localizar la traza
/// completa sin exponer detalles internos en la respuesta.
/// </param>
public sealed record RespuestaDeError(
    int Codigo,
    string Mensaje,
    IReadOnlyList<DetalleDeError> Detalles,
    string Traza)
{
    /// <summary>Construye una respuesta sin detalles por campo.</summary>
    /// <param name="codigo">Codigo HTTP de la respuesta.</param>
    /// <param name="mensaje">Texto apto para mostrar al usuario final.</param>
    /// <param name="traza">Identificador unico del error.</param>
    public static RespuestaDeError Simple(int codigo, string mensaje, string traza) =>
        new(codigo, mensaje, Array.Empty<DetalleDeError>(), traza);
}

/// <summary>
/// Error asociado a un campo concreto de la solicitud. Solo se emite en las
/// respuestas 400 producidas por FluentValidation (RN-15).
/// </summary>
/// <param name="Campo">Nombre de la propiedad del DTO que no supero la validacion.</param>
/// <param name="Error">Descripcion del incumplimiento.</param>
public sealed record DetalleDeError(string Campo, string Error);
