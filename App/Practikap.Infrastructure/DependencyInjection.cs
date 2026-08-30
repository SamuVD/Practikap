using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Practikap.Application.Common;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;
using Practikap.Infrastructure.Repositories;
using Practikap.Infrastructure.Security;

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
    /// Registra el contexto de EF Core, los servicios de seguridad y el esquema
    /// de autenticacion JWT.
    /// </summary>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    /// <param name="configuration">Configuracion desde la que se leen la cadena de conexion y la seccion Jwt.</param>
    /// <returns>La misma coleccion, para permitir encadenamiento.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si la cadena de conexion o alguna clave de la seccion Jwt no esta definida.
    /// Se lanza aqui, con un mensaje que nombra el archivo que falta, en lugar de
    /// dejar que la aplicacion falle mas tarde sin contexto.
    /// </exception>
    /// <remarks>
    /// La autenticacion se registra en esta capa y no en Practikap.API porque la
    /// validacion del token consulta ITokenRevocadoRepository: ubicarla en la API
    /// obligaria a que la capa de presentacion conociera un repositorio, que es
    /// justo lo que el criterio de aceptacion de RNF-08 verifica que no ocurra.
    /// Coincide ademas con Doc_Tecnico 2.2 y con Doc_Arquitectura 8 (RN-02).
    /// </remarks>
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

        // Unidad de trabajo (ADR-02): punto unico de confirmacion de la peticion.
        // Comparte el alcance Scoped del DbContext, del que toma la instancia.
        services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();

        // Repositorios concretos: Fase 4, uno por modulo. La unica excepcion es
        // TokenRevocadoRepository, que consume el pipeline y no un caso de uso.
        services.AddScoped<ITokenRevocadoRepository, TokenRevocadoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRolRepository, RolRepository>();

        // Modulo M3. IProgramaRepository se registra aqui una sola vez: lo
        // consumen M3, que consulta, y M8, que administra.
        services.AddScoped<IPracticaRepository, PracticaRepository>();
        services.AddScoped<IFichaRepository, FichaRepository>();
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IProgramaRepository, ProgramaRepository>();

        // Modulo M4. Los dos contratos son de escritura acotada: registran y
        // marcan como anulado, nunca editan ni eliminan (RN-12).
        services.AddScoped<ISeguimientoRepository, SeguimientoRepository>();
        services.AddScoped<IObservacionRepository, ObservacionRepository>();

        var opcionesJwt = OpcionesJwt.Leer(configuration);
        services.AddSingleton(opcionesJwt);

        services.AddHttpContextAccessor();
        services.AddScoped<IContextoUsuario, ContextoUsuario>();
        services.AddScoped<IGeneradorDeToken, GeneradorDeTokenJwt>();
        services.AddScoped<IServicioDeHash, HasherBCrypt>();

        RegistrarAutenticacion(services, opcionesJwt);
        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Configura el esquema JwtBearer: validacion de firma, emisor, audiencia y
    /// vigencia (RN-02), consulta de la lista de revocacion (RN-03) y emision de
    /// las respuestas 401 y 403 con el contrato del Doc_Tecnico 5.9.
    /// </summary>
    private static void RegistrarAutenticacion(IServiceCollection services, OpcionesJwt opciones)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(configurar =>
            {
                // Sin esto, ASP.NET Core reescribe 'sub' y 'role' como las URI
                // largas de ClaimTypes y el resto del sistema dejaria de ver los
                // nombres cortos que documenta el Doc_Tecnico 3.2.
                configurar.MapInboundClaims = false;

                configurar.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(opciones.SecretKey)),

                    ValidateIssuer = true,
                    ValidIssuer = opciones.Issuer,

                    ValidateAudience = true,
                    ValidAudience = opciones.Audience,

                    ValidateLifetime = true,

                    // Sin tolerancia de reloj. El valor por defecto son cinco
                    // minutos, que dejarian vivo un token ya expirado y harian no
                    // determinista cualquier prueba de expiracion (RNF-04).
                    ClockSkew = TimeSpan.Zero,

                    // Coherente con MapInboundClaims = false.
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };

                configurar.Events = new JwtBearerEvents
                {
                    OnTokenValidated = VerificarRevocacionAsync,
                    OnChallenge = ResponderNoAutenticadoAsync,
                    OnForbidden = ResponderProhibidoAsync
                };
            });
    }

    /// <summary>
    /// Rechaza los tokens revocados. Se ejecuta solo despues de que la firma
    /// resulto valida, que es la condicion que fija el Doc_Tecnico 3.2 para no
    /// consultar la base de datos en cada peticion (RN-03).
    /// </summary>
    private static async Task VerificarRevocacionAsync(TokenValidatedContext contexto)
    {
        var referencia = contexto.Principal?.FindFirst("jti")?.Value;

        if (string.IsNullOrWhiteSpace(referencia))
        {
            contexto.Fail("El token no contiene el claim jti y no puede verificarse contra la lista de revocacion.");
            return;
        }

        var repositorio = contexto.HttpContext.RequestServices
            .GetRequiredService<ITokenRevocadoRepository>();

        if (await repositorio.EstaRevocadoAsync(referencia, contexto.HttpContext.RequestAborted))
            contexto.Fail("El token fue revocado.");
    }

    /// <summary>
    /// Escribe el 401 con el contrato uniforme. Sin esto, JwtBearer cortocircuita
    /// el pipeline antes del middleware global de errores y responde con cuerpo
    /// vacio, incumpliendo el Doc_Tecnico 3.3.
    /// </summary>
    private static Task ResponderNoAutenticadoAsync(JwtBearerChallengeContext contexto)
    {
        contexto.HandleResponse();

        return EscribirErrorAsync(
            contexto.HttpContext,
            StatusCodes.Status401Unauthorized,
            "Token ausente, malformado, expirado o revocado.");
    }

    /// <summary>
    /// Escribe el 403 con el contrato uniforme, para el caso de un token valido
    /// cuyo rol no alcanza para el endpoint solicitado (RN-01).
    /// </summary>
    private static Task ResponderProhibidoAsync(ForbiddenContext contexto) =>
        EscribirErrorAsync(
            contexto.HttpContext,
            StatusCodes.Status403Forbidden,
            "El rol autenticado no tiene permiso sobre este recurso.");

    private static Task EscribirErrorAsync(HttpContext contexto, int codigo, string mensaje)
    {
        if (contexto.Response.HasStarted)
            return Task.CompletedTask;

        contexto.Response.StatusCode = codigo;
        contexto.Response.ContentType = "application/json; charset=utf-8";

        var respuesta = RespuestaDeError.Simple(codigo, mensaje, Guid.NewGuid().ToString());

        return contexto.Response.WriteAsJsonAsync(respuesta);
    }
}
