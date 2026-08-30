using AutoMapper;
using Practikap.Application.DTOs.Calificaciones;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>
/// Mapeos de las dos entidades del modulo M5 hacia su DTO de salida comun.
/// </summary>
/// <remarks>
/// Dos mapas hacia el mismo destino, uno por direccion. No necesita ningun
/// ForCtorParam: los nombres de los parametros de CalificacionResponse coinciden
/// con los de las propiedades de las dos entidades.
///
/// CalificacionesDePracticaResponse no se declara aqui. No proyecta desde una
/// entidad sino que compone dos listas y dos promedios que salen de cuatro
/// consultas distintas, de modo que lo arma el caso de uso a mano.
///
/// Ningun CreateMap desde Usuario, igual que en M3 y M4. Aqui la razon es la de
/// M4 y no la de M3: las dos configuraciones mapean anulado_por sin propiedad de
/// navegacion, asi que no existe un Usuario en el grafo del que se pudiera mapear
/// de mas (RNF-05, H32).
/// </remarks>
public sealed class CalificacionMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public CalificacionMappingProfile()
    {
        CreateMap<CalificacionInstructor, CalificacionResponse>();
        CreateMap<CalificacionAprendiz, CalificacionResponse>();
    }
}
