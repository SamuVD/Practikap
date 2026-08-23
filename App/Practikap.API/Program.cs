using Microsoft.OpenApi;
using Practikap.API.Middlewares;
using Practikap.Application;
using Practikap.Infrastructure;
using Practikap.Infrastructure.Persistence;

const string PoliticaCors = "FrontendPractikap";

var builder = WebApplication.CreateBuilder(args);

// Secretos locales. appsettings.{Entorno}.local.json esta ignorado por el
// .gitignore y sobreescribe lo que traiga appsettings.{Entorno}.json, porque las
// fuentes de configuracion registradas despues tienen prioridad. Es opcional
// para que el pipeline de la Fase 7, que inyecta la cadena como variable de
// entorno y no tiene este archivo, no falle al arrancar (Doc_Tecnico 6.2).
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

// Add services to the container.

builder.Services.AddControllers();

// ADR-05: cada capa declara sus propios registros. Program.cs no conoce ninguna
// clase concreta de las capas internas.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- CORS (Doc_Tecnico 6.2) -------------------------------------------------
// El origen llega como una unica cadena, que es la forma que toma la variable de
// entorno CORS__AllowedOrigins en produccion. Varios origenes se separan por
// coma. No se habilitan credenciales: el token viaja en la cabecera
// Authorization, no en una cookie.
var origenesPermitidos = (builder.Configuration["CORS:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

if (origenesPermitidos.Length == 0)
{
    throw new InvalidOperationException(
        "La clave de configuracion 'CORS:AllowedOrigins' esta vacia o no existe.\n" +
        "Agreguela a App/Practikap.API/appsettings.Development.json con los origenes\n" +
        "del frontend separados por coma. No es un secreto: se versiona.");
}

builder.Services.AddCors(opciones => opciones.AddPolicy(PoliticaCors, politica => politica
    .WithOrigins(origenesPermitidos)
    .AllowAnyHeader()
    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")));

// --- Swagger / OpenAPI (Doc_Stack 4.8) --------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Practikap API",
        Version = "v1",
        Description = "API REST de gestion de practicas productivas."
    });

    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Pegue aqui el token, sin la palabra Bearer.",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    opciones.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", documento)] = new List<string>()
    });

    opciones.OperationFilter<FiltroDeOperacionAnonima>();

    // Comentarios XML de las tres capas que los generan (Doc_Tecnico 5.10).
    foreach (var ensamblado in new[] { "Practikap.Domain", "Practikap.Application", "Practikap.API" })
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, $"{ensamblado}.xml");
        if (File.Exists(ruta))
            opciones.IncludeXmlComments(ruta, includeControllerXmlComments: true);
    }
});

var app = builder.Build();

// --- Sembrado de cuentas de desarrollo (Ronda 2, Fase 4.1) ------------------
// Solo en Development y solo si SeedUsuarios existe en la configuracion local.
// Ver SembradorUsuariosDesarrollo para la logica completa.
if (app.Environment.IsDevelopment())
{
    await SembradorUsuariosDesarrollo.SembrarAsync(app.Services);
}

// --- Pipeline (Doc_Tecnico 3.1) ---------------------------------------------
// El orden es normativo. El manejador de errores va primero porque solo captura
// lo que ocurre despues de el; CORS va antes de la autenticacion para que una
// peticion de origen no permitido se rechace sin llegar a validar el token.
app.UseManejadorDeErrores();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opciones =>
        opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Practikap API v1"));
}

app.UseHttpsRedirection();
app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
