using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Practikap.Application.UseCases.Autenticacion;
using Practikap.Application.UseCases.Calificaciones;
using Practikap.Application.UseCases.Empresas;
using Practikap.Application.UseCases.Fichas;
using Practikap.Application.UseCases.Mensajes;
using Practikap.Application.UseCases.Observaciones;
using Practikap.Application.UseCases.Practicas;
using Practikap.Application.UseCases.Programas;
using Practikap.Application.UseCases.Roles;
using Practikap.Application.UseCases.Seguimientos;
using Practikap.Application.UseCases.Usuarios;

namespace Practikap.Application;

/// <summary>
/// Punto unico de registro de la capa de Aplicacion en el contenedor de
/// dependencias, segun ADR-05. Practikap.API invoca AddApplication y no necesita
/// conocer ninguna clase concreta de esta capa.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra los perfiles de AutoMapper y los validadores de FluentValidation
    /// declarados en este ensamblado.
    /// </summary>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    /// <returns>La misma coleccion, para permitir encadenamiento.</returns>
    /// <remarks>
    /// El registro es por escaneo de ensamblado y no por enumeracion manual: cada
    /// modulo de la Fase 4 agrega sus perfiles y validadores sin tocar este
    /// archivo, que es el criterio de aceptacion de RNF-09. El escaneo ya
    /// encuentra los perfiles y validadores del modulo M1.
    ///
    /// Los casos de uso si se enumeran uno por uno, con alcance Scoped, porque
    /// dependen del DbContext (ADR-02). El modulo M1 aporta los once primeros.
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var ensamblado = typeof(DependencyInjection).Assembly;

        services.AddAutoMapper(configuracion => configuracion.AddMaps(ensamblado));
        services.AddValidatorsFromAssembly(ensamblado, includeInternalTypes: true);

        // Casos de uso, uno por uno y con alcance Scoped: dependen de
        // repositorios que comparten el DbContext de la peticion (ADR-02).
        // Modulo M1 - Autenticacion.
        services.AddScoped<IniciarSesionUseCase>();
        services.AddScoped<CerrarSesionUseCase>();
        // Modulo M1 - Usuarios y roles.
        services.AddScoped<ListarUsuariosUseCase>();
        services.AddScoped<ObtenerUsuarioUseCase>();
        services.AddScoped<CrearUsuarioUseCase>();
        services.AddScoped<ActualizarPerfilUseCase>();
        services.AddScoped<CambiarContrasenaUseCase>();
        services.AddScoped<RestablecerContrasenaUseCase>();
        services.AddScoped<CambiarRolUseCase>();
        services.AddScoped<CambiarEstadoUsuarioUseCase>();
        services.AddScoped<ListarRolesUseCase>();
        // Modulo M3 - Practicas.
        services.AddScoped<CrearPracticaUseCase>();
        services.AddScoped<ActualizarPracticaUseCase>();
        services.AddScoped<CambiarEstadoPracticaUseCase>();
        services.AddScoped<ListarPracticasUseCase>();
        services.AddScoped<ObtenerPracticaUseCase>();
        // Modulo M3 - Datos maestros que gestiona el Administrador (FA-26).
        services.AddScoped<CrearProgramaUseCase>();
        services.AddScoped<ListarProgramasUseCase>();
        services.AddScoped<CrearFichaUseCase>();
        services.AddScoped<ListarFichasUseCase>();
        services.AddScoped<CrearEmpresaUseCase>();
        services.AddScoped<ListarEmpresasUseCase>();
        // Modulo M4 - Seguimiento y observaciones.
        services.AddScoped<RegistrarSeguimientoUseCase>();
        services.AddScoped<ObtenerSeguimientoUseCase>();
        services.AddScoped<AnularSeguimientoUseCase>();
        services.AddScoped<ListarSeguimientosDePracticaUseCase>();
        services.AddScoped<RegistrarObservacionUseCase>();
        services.AddScoped<AnularObservacionUseCase>();
        // Modulo M5 - Calificacion bidireccional.
        services.AddScoped<RegistrarCalificacionInstructorUseCase>();
        services.AddScoped<RegistrarCalificacionAprendizUseCase>();
        services.AddScoped<ListarCalificacionesDePracticaUseCase>();
        services.AddScoped<AnularCalificacionInstructorUseCase>();
        services.AddScoped<AnularCalificacionAprendizUseCase>();
        // Modulo M6 - Mensajeria. Las notificaciones son el paso 4.6.
        services.AddScoped<EnviarMensajeUseCase>();
        services.AddScoped<ListarMensajesDePracticaUseCase>();
        services.AddScoped<MarcarMensajeLeidoUseCase>();

        return services;
    }
}
