using Practikap.Infrastructure;

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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// ADR-05: cada capa declara sus propios registros. AddApplication() entra en la
// Ronda 5, cuando la capa de Aplicacion tenga casos de uso que registrar.
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
