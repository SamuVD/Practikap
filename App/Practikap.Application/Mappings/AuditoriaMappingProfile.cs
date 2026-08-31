using AutoMapper;
using Practikap.Application.DTOs.Auditoria;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>Mapeo del asiento de auditoria hacia su DTO de salida.</summary>
/// <remarks>
/// Un solo mapa. Todos los nombres coinciden salvo EntidadAfectada y Accion, que se
/// proyectan a texto con ToString(), igual que PracticaMappingProfile proyecta Estado
/// y Modalidad, NotificacionMappingProfile proyecta Tipo y ReglaMappingProfile
/// proyecta Operador (H31).
///
/// Como en el caso del operador de M2, <b>el literal que resulta no es el que guarda
/// la columna</b>: los miembros se llaman RetrocesoEstado y Practicas, y las columnas
/// guardan "Retroceso_estado" y "practicas". La traduccion entre ambos es de
/// ConvertidoresDeEnum y no sale de la Infraestructura. El contrato de la API es el
/// nombre del miembro, que es tambien el que los dos filtros del GET aceptan de
/// entrada: lo que se lee se puede reenviar sin traducir.
///
/// Ningun CreateMap desde Usuario, y aqui es la entidad la que lo garantiza: ADR-06
/// fija que RegistroAuditoria no declara ninguna propiedad de navegacion, ni hacia el
/// actor ni hacia el objeto afectado. No hay grafo que pudiera arrastrar un Usuario
/// (RNF-05, H32).
///
/// Tampoco declara mapa de entrada. No existe DTO de entrada: nada de esta ronda
/// escribe en la bitacora, y cuando la Ronda 2 lo haga sera construyendo la entidad
/// por constructor desde el registrador, no mapeando un cuerpo HTTP. Un asiento que
/// se pudiera componer desde fuera no seria una bitacora.
/// </remarks>
public sealed class AuditoriaMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public AuditoriaMappingProfile()
    {
        CreateMap<RegistroAuditoria, RegistroAuditoriaResponse>()
            .ForCtorParam("EntidadAfectada",
                opciones => opciones.MapFrom(registro => registro.EntidadAfectada.ToString()))
            .ForCtorParam("Accion",
                opciones => opciones.MapFrom(registro => registro.Accion.ToString()));
    }
}
