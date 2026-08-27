using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Domain.Entities;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Infrastructure.Persistence;

/// <summary>
/// Siembra un usuario por rol en entornos de Development, para tener las tres
/// cuentas de prueba disponibles sin crearlas a mano en cada clon del repositorio.
/// </summary>
/// <remarks>
/// No se ejecuta como parte de la migracion: correr una sola vez al arrancar la
/// API mantiene el sembrado independiente del ciclo de vida del esquema y evita
/// que un hash de contrasena de prueba viaje dentro de un archivo de migracion
/// versionado. En produccion (Fase 7) el primer Administrador se crea con un
/// INSERT manual documentado en el checklist de despliegue.
/// </remarks>
public static class SembradorUsuariosDesarrollo
{
    /// <summary>Nombre de la seccion de configuracion que declara las cuentas a sembrar.</summary>
    private const string Seccion = "SeedUsuarios";

    /// <summary>
    /// Los tres roles que se intentan sembrar. El nombre debe coincidir
    /// exactamente con el sembrado por RolConfiguration.HasData.
    /// </summary>
    private static readonly string[] Roles = { "Administrador", "Instructor", "Aprendiz" };

    /// <summary>
    /// Recorre <see cref="Roles"/> y crea la cuenta correspondiente si la
    /// seccion de configuracion la declara y el correo aun no existe.
    /// </summary>
    /// <param name="servicios">Proveedor de servicios raiz de la aplicacion.</param>
    /// <param name="ct">Token de cancelacion del arranque.</param>
    /// <remarks>
    /// Se ejecuta fuera de cualquier peticion HTTP, asi que crea su propio
    /// alcance (CreateScope) para resolver los servicios Scoped que necesita.
    /// Es idempotente: una cuenta ya existente por correo se omite sin error.
    /// </remarks>
    public static async Task SembrarAsync(IServiceProvider servicios, CancellationToken ct = default)
    {
        using var alcance = servicios.CreateScope();
        var proveedor = alcance.ServiceProvider;

        var configuracion = proveedor.GetRequiredService<IConfiguration>();
        var registro = proveedor.GetRequiredService<ILogger<PractikapDbContext>>();

        if (!configuracion.GetSection(Seccion).Exists())
        {
            registro.LogWarning(
                "La seccion '{Seccion}' no existe en la configuracion. No se sembro ninguna cuenta de prueba. " +
                "Agreguela a appsettings.Development.local.json si necesita las tres cuentas de desarrollo.",
                Seccion);
            return;
        }

        var usuarioRepo = proveedor.GetRequiredService<IUsuarioRepository>();
        var rolRepo = proveedor.GetRequiredService<IRolRepository>();
        var hasher = proveedor.GetRequiredService<IServicioDeHash>();
        var unidadDeTrabajo = proveedor.GetRequiredService<IUnidadDeTrabajo>();

        var sembradas = 0;

        foreach (var nombreRol in Roles)
        {
            var correo = configuracion[$"{Seccion}:{nombreRol}:Correo"];
            var contrasena = configuracion[$"{Seccion}:{nombreRol}:Contrasena"];

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
            {
                registro.LogWarning(
                    "'{Seccion}:{Rol}' esta incompleta (falta Correo o Contrasena). Se omite esa cuenta.",
                    Seccion, nombreRol);
                continue;
            }

            if (await usuarioRepo.ExisteCorreoAsync(correo, ct))
            {
                registro.LogInformation("La cuenta de prueba {Rol} ({Correo}) ya existe. Se omite.", nombreRol, correo);
                continue;
            }

            var rol = await rolRepo.ObtenerPorNombreAsync(nombreRol, ct)
                ?? throw new InvalidOperationException(
                    $"El rol '{nombreRol}' no esta sembrado en la tabla roles. " +
                    "Verifique que la migracion InicialPractikap se aplico correctamente.");

            try
            {
                var hash = hasher.Hash(contrasena);
                var usuario = new Usuario(rol.Id, correo, hash, nombreRol, "Practikap");

                await usuarioRepo.AgregarAsync(usuario, ct);
                sembradas++;
            }
            catch (ReglaDeDominioException excepcion)
            {
                registro.LogWarning(
                    "No se pudo sembrar la cuenta {Rol} ({Correo}): {Mensaje}",
                    nombreRol, correo, excepcion.Message);
            }
        }

        if (sembradas > 0)
        {
            await unidadDeTrabajo.GuardarCambiosAsync(ct);
            registro.LogInformation("Sembradas {Cantidad} cuenta(s) de prueba.", sembradas);
        }
    }
}