using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Practikap.Application.Common;
using Practikap.Application.UseCases.Auditoria;
using Practikap.Application.UseCases.Autenticacion;
using Practikap.Application.UseCases.Calificaciones;
using Practikap.Application.UseCases.Configuracion;
using Practikap.Application.UseCases.Empresas;
using Practikap.Application.UseCases.Fichas;
using Practikap.Application.UseCases.Mensajes;
using Practikap.Application.UseCases.Notificaciones;
using Practikap.Application.UseCases.Observaciones;
using Practikap.Application.UseCases.Practicas;
using Practikap.Application.UseCases.Programas;
using Practikap.Application.UseCases.Reglas;
using Practikap.Application.UseCases.Reportes;
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
    ///
    /// Las dos piezas que no son ni perfil, ni validador, ni caso de uso van al
    /// final, registradas por la misma razon de alcance:
    /// IGeneradorDeNotificaciones, que aporto el paso 4.6 (L6), y
    /// IEvaluadorDeReglas, que aporta la Ronda 2 del 4.7 (N11).
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
        // Modulo M6 - Mensajeria, paso 4.5.
        services.AddScoped<EnviarMensajeUseCase>();
        services.AddScoped<ListarMensajesDePracticaUseCase>();
        services.AddScoped<MarcarMensajeLeidoUseCase>();
        // Modulo M6 - Notificaciones, paso 4.6. El modulo se reparte entre dos
        // pasos (Doc_Arquitectura 7.1) y este lo cierra.
        services.AddScoped<ListarNotificacionesUseCase>();
        services.AddScoped<CrearNotificacionAdministrativaUseCase>();
        services.AddScoped<MarcarNotificacionLeidaUseCase>();
        // Modulo M2 - Motor de Reglas, paso 4.7. MotorDeReglas no aparece aqui: es
        // estatico, sin estado y vive en el Dominio (ADR-04). Lo unico que necesita
        // alcance Scoped es lo que carga y persiste las reglas, y el servicio que
        // lo dispara, que se registra mas abajo con los otros dos que no son casos
        // de uso.
        services.AddScoped<CrearReglaUseCase>();
        services.AddScoped<ListarReglasUseCase>();
        services.AddScoped<ObtenerReglaUseCase>();
        services.AddScoped<ActualizarReglaUseCase>();
        services.AddScoped<CambiarActivaReglaUseCase>();
        // Modulo M7 - Reportes y exportacion, paso 4.8. SerializadorDeFiltro,
        // ArmadorDeReporte y FormateadorCsv no aparecen aqui: son estaticos, sin
        // estado y sin dependencias, con el mismo criterio que
        // ParticipantesDePractica.
        //
        // ExportarReporteUseCase es el unico caso de uso del proyecto que inyecta
        // otro caso de uso (O23). Se registra igual que los demas, y funciona
        // porque los dos son Scoped: comparten el DbContext de la peticion.
        services.AddScoped<GenerarReporteUseCase>();
        services.AddScoped<ListarReportesUseCase>();
        services.AddScoped<ObtenerReporteUseCase>();
        services.AddScoped<ExportarReporteUseCase>();
        // Modulo M8 - Panel de Administracion, paso 4.9. Cuatro casos de uso y no
        // mas: M8 no reimplementa usuarios, programas ni reglas, que cerraron los
        // pasos 4.1, 4.2 y 4.7 (P1). ReglasDeConfiguracion no aparece aqui: es
        // estatica, sin estado y sin dependencias, con el mismo criterio que
        // ReglasDeMotor y ReglasDeEnumerado.
        services.AddScoped<ListarConfiguracionUseCase>();
        services.AddScoped<ObtenerConfiguracionUseCase>();
        services.AddScoped<EstablecerConfiguracionUseCase>();
        services.AddScoped<ListarAuditoriaUseCase>();

        // Punto unico de emision de notificaciones (L6). No es un caso de uso,
        // pero se enumera a mano por el mismo motivo que ellos: tiene alcance
        // Scoped porque su repositorio comparte el DbContext de la peticion
        // (ADR-02, ADR-05). Es lo que permite que la notificacion de un evento se
        // confirme en la misma transaccion que el evento.
        services.AddScoped<IGeneradorDeNotificaciones, GeneradorDeNotificaciones>();

        // Punto unico de disparo del Motor de Reglas (N11), enumerado a mano por
        // la misma razon de alcance: sus seis colaboradores comparten el DbContext
        // de la peticion (ADR-02, ADR-05). Es lo que permite que el cambio de
        // estado de la practica y su notificacion de riesgo se confirmen en la
        // misma transaccion que la calificacion que los disparo.
        services.AddScoped<IEvaluadorDeReglas, EvaluadorDeReglas>();

        // Punto unico de escritura de la bitacora (P12), la tercera pieza de
        // Aplicacion que no es caso de uso y la primera que conoce al actor. Mismo
        // alcance y misma razon que las dos de arriba: su repositorio comparte el
        // DbContext de la peticion (ADR-02, ADR-05), y es lo que permite que una
        // accion sensible y su asiento se confirmen en la misma transaccion.
        services.AddScoped<IRegistradorDeAuditoria, RegistradorDeAuditoria>();

        return services;
    }
}
