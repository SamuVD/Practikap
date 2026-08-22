using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Practikap.API.Middlewares;

/// <summary>
/// Retira el candado de Swagger UI de los endpoints marcados con [AllowAnonymous].
/// </summary>
/// <remarks>
/// AddSecurityRequirement se declara a nivel de documento completo (Etapa 5), asi
/// que sin este filtro Swagger mostraria el candado en todos los endpoints,
/// incluidos los que no exigen token. No es un fallo de seguridad -el 401 real lo
/// decide el middleware de autenticacion, no el icono- pero la documentacion debe
/// reflejar con precision cuales endpoints piden Authorization (Doc_Tecnico 3.3).
/// </remarks>
public sealed class FiltroDeOperacionAnonima : IOperationFilter
{
    /// <summary>Quita los requisitos de seguridad de la operacion si es anonima.</summary>
    /// <param name="operacion">Operacion de OpenAPI en construccion.</param>
    /// <param name="contexto">Contexto con los metadatos del endpoint.</param>
    public void Apply(OpenApiOperation operacion, OperationFilterContext contexto)
    {
        var esAnonimo = contexto.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any();

        if (esAnonimo)
            operacion.Security = new List<OpenApiSecurityRequirement>();
    }
}