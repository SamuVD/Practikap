using AutoMapper;
using Practikap.Application.DTOs.Reportes;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>Mapeo de la entidad de reportes hacia su DTO de listado.</summary>
/// <remarks>
/// Un solo mapa, y solo el del rastro. Todos los nombres coinciden salvo Tipo,
/// que se proyecta a texto con ToString(), igual que PracticaMappingProfile
/// proyecta Estado y Modalidad y ReglaMappingProfile proyecta Operador (H31).
/// Aqui el literal que resulta si coincide con el que guarda la columna, porque
/// ConvertidorTipoReporte traduce Individual y Grupal a si mismos.
///
/// ReporteResponse no tiene mapa y no por olvido: sus dos ultimos campos son las
/// lineas y los totales, que no salen de la entidad sino de componer sus practicas
/// con dos diccionarios de promedios que AutoMapper no tiene de donde sacar. Lo
/// arma ArmadorDeReporte y lo devuelve el caso de uso.
///
/// Filtros se mapea por nombre y sin conversion: sale el mismo texto JSON que
/// guarda la columna. Deserializarlo aqui a su forma tipada habria hecho que una
/// fila escrita a mano en MySQL por fuera de la API tumbara el listado entero.
///
/// Ningun CreateMap desde Usuario, igual que en M3, M4, M5, M6 y M2. Reporte no
/// declara navegacion hacia el generador —GeneradoPor es la clave foranea y
/// basta—, y aunque ReporteRepository.ObtenerPorIdAsync si carga los Usuario de
/// instructor y aprendiz dentro de cada practica, lo unico que sale de ahi es
/// NombreCompleto, proyectado a mano en LineaDeReporteResponse (RNF-05, H32).
///
/// Tampoco declara mapas desde los DTO de entrada hacia Reporte. La entidad se
/// construye por constructor y se compone con VincularPractica, que son sus unicas
/// fuentes: un mapa de entrada podria escribir GeneradoPor o FechaGeneracion
/// saltandose las invariantes y RN-11.
/// </remarks>
public sealed class ReporteMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public ReporteMappingProfile()
    {
        CreateMap<Reporte, ReporteResumenResponse>()
            .ForCtorParam("Tipo",
                opciones => opciones.MapFrom(reporte => reporte.Tipo.ToString()));
    }
}
