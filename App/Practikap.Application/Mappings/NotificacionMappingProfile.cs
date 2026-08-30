using AutoMapper;
using Practikap.Application.DTOs.Notificaciones;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>Mapeo de la entidad de notificaciones hacia su DTO de salida.</summary>
/// <remarks>
/// Un solo mapa. Todos los nombres coinciden salvo Tipo, que se proyecta a texto
/// con ToString(), igual que PracticaMappingProfile proyecta Estado y Modalidad
/// (H31). El literal que resulta es identico al que guarda la columna ENUM,
/// porque los miembros de TipoNotificacion se llaman como sus literales: la
/// columna es de las que no llevan tilde ni espacio, que es la divergencia que
/// FA-10 dejo abierta para v2.
///
/// Ningun CreateMap desde Usuario ni desde Regla, igual que en M3, M4, M5 y la
/// mensajeria del 4.5. Aqui la razon es la mas fuerte: el destinatario esta
/// mapeado como clave foranea sin navegacion, y la unica navegacion que existe
/// —Regla— no la carga ninguna consulta del repositorio (RNF-05, H32).
///
/// Tampoco declara un mapa desde CrearNotificacionRequest hacia Notificacion. La
/// entidad se construye en GeneradorDeNotificaciones, que es el punto unico de
/// emision (L6), y uno de sus tres argumentos —el tipo— no viene del DTO.
/// </remarks>
public sealed class NotificacionMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public NotificacionMappingProfile()
    {
        CreateMap<Notificacion, NotificacionResponse>()
            .ForCtorParam("Tipo",
                opciones => opciones.MapFrom(notificacion => notificacion.Tipo.ToString()));
    }
}
