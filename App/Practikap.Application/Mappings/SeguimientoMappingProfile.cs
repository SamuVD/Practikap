using AutoMapper;
using Practikap.Application.DTOs.Observaciones;
using Practikap.Application.DTOs.Seguimientos;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>
/// Mapeos de las dos entidades del modulo M4 hacia sus DTO de salida.
/// </summary>
/// <remarks>
/// No necesita un solo ForCtorParam: los nombres de los parametros de los dos
/// registros coinciden con los de las propiedades de las entidades, y la
/// coleccion anidada de I5 la resuelve AutoMapper con el segundo mapa de este
/// mismo perfil.
///
/// Ningun CreateMap desde Usuario, igual que en M3. Aqui la razon es mas fuerte
/// que una convencion: las dos configuraciones mapean anulado_por sin propiedad
/// de navegacion, asi que no existe un Usuario en el grafo del que se pudiera
/// mapear de mas (RNF-05).
///
/// El origen de SeguimientoResponse debe traer Observaciones cargada. Las dos
/// consultas de lectura de SeguimientoRepository lo hacen, y el caso de uso de
/// alta relee con ObtenerPorIdAsync despues de confirmar.
/// </remarks>
public sealed class SeguimientoMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public SeguimientoMappingProfile()
    {
        CreateMap<Seguimiento, SeguimientoResponse>();
        CreateMap<Observacion, ObservacionResponse>();
    }
}
