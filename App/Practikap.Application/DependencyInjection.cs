using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

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
    /// archivo, que es el criterio de aceptacion de RNF-09. Hoy el escaneo no
    /// encuentra nada, y eso es correcto: la capa aun no tiene casos de uso.
    ///
    /// Los casos de uso se registran aqui uno por uno a partir de la Fase 4.1,
    /// con alcance Scoped, porque dependen del DbContext (ADR-02).
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var ensamblado = typeof(DependencyInjection).Assembly;

        services.AddAutoMapper(configuracion => configuracion.AddMaps(ensamblado));
        services.AddValidatorsFromAssembly(ensamblado, includeInternalTypes: true);

        return services;
    }
}
