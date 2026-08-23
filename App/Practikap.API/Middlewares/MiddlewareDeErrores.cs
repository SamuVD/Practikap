using FluentValidation;
using Practikap.Application.Common;
using Practikap.Domain.Exceptions;

namespace Practikap.API.Middlewares;

/// <summary>
/// Traduce las excepciones de dominio al contrato de error uniforme del
/// Doc_Tecnico 5.9. Es el unico punto del sistema que convierte una excepcion en
/// un codigo HTTP: los casos de uso no conocen el protocolo de transporte.
/// </summary>
public sealed class MiddlewareDeErrores
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<MiddlewareDeErrores> _registro;

    /// <summary>Crea el middleware.</summary>
    /// <param name="siguiente">Siguiente componente del pipeline.</param>
    /// <param name="registro">Registro de eventos de la aplicacion.</param>
    public MiddlewareDeErrores(RequestDelegate siguiente, ILogger<MiddlewareDeErrores> registro)
    {
        _siguiente = siguiente;
        _registro = registro;
    }

    /// <summary>Ejecuta el resto del pipeline y captura cualquier excepcion.</summary>
    /// <param name="contexto">Contexto de la peticion HTTP.</param>
    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (Exception excepcion)
        {
            await ResponderAsync(contexto, excepcion);
        }
    }

    private async Task ResponderAsync(HttpContext contexto, Exception excepcion)
    {
        var traza = Guid.NewGuid().ToString();
        var (codigo, mensaje, detalles) = Traducir(excepcion);

        // El middleware global es el unico punto que registra en nivel Error
        // (Doc_Tecnico 5.11). Todo lo demas es una solicitud rechazada, que es
        // Warning. La traza se escribe con el mismo valor que se devuelve.
        if (codigo >= StatusCodes.Status500InternalServerError)
        {
            _registro.LogError(
                excepcion,
                "Fallo no controlado en {Metodo} {Ruta}. Traza {Traza}",
                contexto.Request.Method, contexto.Request.Path, traza);
        }
        else
        {
            _registro.LogWarning(
                "Solicitud rechazada en {Metodo} {Ruta} con codigo {Codigo}. Traza {Traza}",
                contexto.Request.Method, contexto.Request.Path, codigo, traza);
        }

        // Si la respuesta ya empezo a escribirse no se puede cambiar el codigo ni
        // el cuerpo: intentarlo lanzaria una segunda excepcion sobre la primera.
        if (contexto.Response.HasStarted)
            return;

        contexto.Response.Clear();
        contexto.Response.StatusCode = codigo;
        contexto.Response.ContentType = "application/json; charset=utf-8";

        await contexto.Response.WriteAsJsonAsync(
            new RespuestaDeError(codigo, mensaje, detalles, traza));
    }

    /// <summary>
    /// Aplica la tabla de traduccion del Doc_Tecnico 5.9. El orden importa: las
    /// excepciones concretas se comprueban antes que DominioException, que es su
    /// clase base.
    /// </summary>
    private static (int Codigo, string Mensaje, IReadOnlyList<DetalleDeError> Detalles) Traducir(
        Exception excepcion) => excepcion switch
        {
            ValidationException validacion => (
                StatusCodes.Status400BadRequest,
                "La solicitud contiene errores de validacion.",
                validacion.Errors
                    .Select(error => new DetalleDeError(error.PropertyName, error.ErrorMessage))
                    .ToArray()),

            NoEncontradoException => (
                StatusCodes.Status404NotFound, excepcion.Message, Vacio),

            ConflictoException => (
                StatusCodes.Status409Conflict, excepcion.Message, Vacio),

            CredencialesInvalidasException => (
                StatusCodes.Status401Unauthorized, excepcion.Message, Vacio),

            AutorizacionException => (
                StatusCodes.Status403Forbidden, excepcion.Message, Vacio),

            ReglaDeDominioException => (
                StatusCodes.Status422UnprocessableEntity, excepcion.Message, Vacio),

            DominioException => (
                StatusCodes.Status422UnprocessableEntity, excepcion.Message, Vacio),

            // Nunca se envia al cliente el mensaje de la excepcion original ni la
            // pila de llamadas en un error 500: solo un mensaje generico y la traza.
            _ => (
                StatusCodes.Status500InternalServerError,
                "Ocurrio un error inesperado. Reporte el identificador de traza al equipo de soporte.",
                Vacio)
        };

    private static IReadOnlyList<DetalleDeError> Vacio => Array.Empty<DetalleDeError>();
}

/// <summary>
/// Registro del middleware de errores en el pipeline.
/// </summary>
public static class ExtensionesDelManejadorDeErrores
{
    /// <summary>
    /// Inserta el manejador global de errores. Debe ser el primero del pipeline:
    /// solo captura lo que ocurre en los componentes que van despues de el.
    /// </summary>
    /// <param name="app">Constructor del pipeline.</param>
    /// <returns>El mismo constructor, para permitir encadenamiento.</returns>
    public static IApplicationBuilder UseManejadorDeErrores(this IApplicationBuilder app) =>
        app.UseMiddleware<MiddlewareDeErrores>();
}