using AutoMapper;
using Practikap.Application.DTOs.Reglas;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>Mapeo de la entidad de reglas hacia su DTO de salida.</summary>
/// <remarks>
/// Un solo mapa. Todos los nombres coinciden salvo Operador, que se proyecta a
/// texto con ToString(), igual que PracticaMappingProfile proyecta Estado y
/// Modalidad y NotificacionMappingProfile proyecta Tipo (H31).
///
/// A diferencia de esos dos, aqui el literal que resulta <b>no</b> es el que guarda
/// la columna: los miembros se llaman Mayor y MayorOIgual, y la columna guarda
/// &gt; y &gt;=. La traduccion entre ambos es de ConvertidoresDeEnum y no sale de
/// la Infraestructura. El contrato de la API es el nombre del miembro, que es
/// tambien el que ReglasDeMotor.ConOperadorValido acepta de entrada: lo que se lee
/// se puede reenviar sin traducir.
///
/// Umbral no se mapea (N3), y no por omision: no existe en ReglaResponse.
///
/// Ningun CreateMap desde Usuario, igual que en M3, M4, M5 y M6. Regla.Creador es
/// navegacion, pero ninguna consulta de ReglaRepository la carga, de modo que
/// CreadoPor sale de la clave foranea y ningun Usuario entra en el grafo (RNF-05,
/// H32).
///
/// Tampoco declara mapas desde los dos DTO de entrada hacia Regla. La entidad se
/// construye por constructor y se modifica por Regla.Actualizar, que son sus unicas
/// fuentes: un mapa de entrada podria escribir Activa o CreadoPor saltandose las
/// invariantes.
/// </remarks>
public sealed class ReglaMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public ReglaMappingProfile()
    {
        CreateMap<Regla, ReglaResponse>()
            .ForCtorParam("Operador",
                opciones => opciones.MapFrom(regla => regla.Operador.ToString()));
    }
}
