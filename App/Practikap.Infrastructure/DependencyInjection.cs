using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure;

/// <summary>
/// Punto unico de registro de la capa de Infraestructura en el contenedor de
/// dependencias, segun ADR-05. Practikap.API invoca AddInfrastructure y no
/// necesita conocer ninguna clase concreta de esta capa.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Clave de la cadena de conexion en la configuracion (Doc_Tecnico 6.2).</summary>
    private const string NombreCadena = "MySQL";

    /// <summary>
    /// Version del motor declarada de forma explicita en lugar de
    /// ServerVersion.AutoDetect. AutoDetect abre una conexion real contra MySQL
    /// tanto en tiempo de diseno como en cada arranque, lo que haria fallar
    /// dotnet ef sin base de datos creada y el pipeline de la Fase 7, que no
    /// tiene un servidor MySQL disponible. La version es la que declaran el
    /// Doc_Stack_Tecnologico y el encabezado del Script_DDL.sql.
    /// </summary>
    private static readonly MySqlServerVersion VersionServidor =
        new(new Version(9, 7, 2));

    /// <summary>
    /// Registra el contexto de EF Core y, mas adelante, los repositorios
    /// concretos y los servicios de seguridad.
    /// </summary>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    /// <param name="configuration">Configuracion desde la que se lee la cadena de conexion.</param>
    /// <returns>La misma coleccion, para permitir encadenamiento.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si la cadena de conexion no esta definida. Se lanza aqui, con un mensaje
    /// que nombra el archivo que falta, en lugar de dejar que Pomelo falle mas
    /// tarde con un error de conexion sin contexto.
    /// </exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cadena = configuration.GetConnectionString(NombreCadena);

        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new InvalidOperationException(
                "La cadena de conexion 'ConnectionStrings:MySQL' esta vacia o no existe.\n" +
                "Cree el archivo App/Practikap.API/appsettings.Development.local.json\n" +
                "copiando el bloque de ejemplo que trae appsettings.Development.json y\n" +
                "reemplazando el usuario y la contrasena por los de su MySQL local.\n" +
                "Ese archivo esta ignorado por .gitignore y nunca debe commitearse.");
        }

        // El ensamblado de migraciones no se declara: por defecto EF Core usa el
        // del DbContext, que es este mismo proyecto.
        services.AddDbContext<PractikapDbContext>(opciones =>
            opciones.UseMySql(cadena, VersionServidor));

        // Repositorios concretos: Fase 4, uno por modulo.
        // Servicios de seguridad (JWT, BCrypt, ContextoUsuario): Ronda 5.

        return services;
    }
}
