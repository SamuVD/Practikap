using AutoMapper;
using Practikap.Application.DTOs.Configuracion;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>Mapeo de la entidad de configuracion hacia su DTO de salida.</summary>
/// <remarks>
/// Un solo mapa y <b>sin una sola conversion</b>, que lo hace el perfil mas simple
/// del proyecto: los cinco campos de ConfiguracionResponse coinciden en nombre y en
/// tipo con los de la entidad. No hay enumerado que proyectar a texto, porque la
/// configuracion es clave y valor en texto libre; lo que acota ese texto es
/// ReglasDeConfiguracion, del lado de la validacion, y no llega a la persistencia
/// como un tipo.
///
/// Id no se mapea, y no por omision: no existe en el DTO. La clave es la identidad
/// publica de la entrada y es lo que viaja en las tres rutas del controlador.
///
/// Ningun CreateMap desde Usuario, igual que en M3, M4, M5, M6, M2 y M7.
/// Configuracion.Actualizador si es navegacion, pero ninguna consulta de
/// ConfiguracionRepository la carga, de modo que ActualizadoPor sale de la clave
/// foranea y ningun Usuario entra en el grafo (RNF-05, H32).
///
/// Tampoco declara un mapa desde EstablecerConfiguracionRequest hacia la entidad. Se
/// construye por constructor y se modifica por Configuracion.Establecer, que son sus
/// unicas fuentes: un mapa de entrada podria escribir Clave, Descripcion o
/// ActualizadoPor saltandose las invariantes y el catalogo de P8.
/// </remarks>
public sealed class ConfiguracionMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public ConfiguracionMappingProfile()
    {
        CreateMap<Configuracion, ConfiguracionResponse>();
    }
}
