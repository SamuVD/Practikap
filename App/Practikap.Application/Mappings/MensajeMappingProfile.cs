using AutoMapper;
using Practikap.Application.DTOs.Mensajes;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>Mapeo de la entidad del modulo M6 hacia su DTO de salida.</summary>
/// <remarks>
/// Un solo mapa, y sin ForCtorParam: los nombres de los parametros de
/// MensajeResponse coinciden con los de las propiedades de Mensaje.
///
/// Ningun CreateMap desde Usuario, igual que en M3, M4 y M5. Aqui la razon es la
/// mas fuerte de las tres: Mensaje mapea emisor_id y receptor_id sin propiedad de
/// navegacion, asi que no existe un Usuario en el grafo del que se pudiera mapear
/// de mas (RNF-05, H32).
///
/// Tampoco declara un mapa desde EnviarMensajeRequest hacia Mensaje. La entidad
/// se construye a mano en el caso de uso, porque dos de sus cuatro argumentos
/// —emisor y receptor— no vienen del DTO sino del token y de la practica (K2).
/// </remarks>
public sealed class MensajeMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public MensajeMappingProfile()
    {
        CreateMap<Mensaje, MensajeResponse>();
    }
}
