using AutoMapper;
using Practikap.Application.DTOs.Practicas;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>
/// Mapeos de la entidad <see cref="Practica"/> hacia su DTO de salida.
/// </summary>
/// <remarks>
/// Aplana el grafo entero a texto. Es deliberado y es lo que impide una fuga:
/// PracticaRepository.ConGrafoCompleto carga el Usuario completo de instructor y
/// aprendiz, ContrasenaHash incluido, y este perfil solo proyecta NombreCompleto.
/// No hay ningun CreateMap desde Usuario en M3, ni debe haberlo (RNF-05).
///
/// El origen debe traer el grafo cargado. Las cuatro consultas de lectura de
/// PracticaRepository lo hacen, y los casos de uso de escritura releen con
/// ObtenerPorIdAsync despues de confirmar, porque una entidad recien insertada o
/// reasignada tiene las navegaciones sin cargar o desactualizadas.
///
/// Programa sale via Ficha: la practica no guarda programa_id, se deriva de
/// ficha_id para mantener la tercera forma normal.
/// </remarks>
public sealed class PracticaMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public PracticaMappingProfile()
    {
        CreateMap<Practica, PracticaResponse>()
            .ForCtorParam("NumeroFicha",
                opciones => opciones.MapFrom(practica => practica.Ficha.NumeroFicha))
            .ForCtorParam("ProgramaId",
                opciones => opciones.MapFrom(practica => practica.Ficha.ProgramaId))
            .ForCtorParam("Programa",
                opciones => opciones.MapFrom(practica => practica.Ficha.Programa.Nombre))
            // Empresa es nula en ProyectoProductivo y Monitoria (H22, H25): la
            // comprobacion va explicita y no se delega en la propagacion de nulos
            // de AutoMapper.
            .ForCtorParam("Empresa",
                opciones => opciones.MapFrom(practica =>
                    practica.Empresa == null ? null : practica.Empresa.RazonSocial))
            .ForCtorParam("Instructor",
                opciones => opciones.MapFrom(practica => practica.Instructor.NombreCompleto))
            .ForCtorParam("Aprendiz",
                opciones => opciones.MapFrom(practica => practica.Aprendiz.NombreCompleto))
            .ForCtorParam("Modalidad",
                opciones => opciones.MapFrom(practica => practica.Modalidad.ToString()))
            .ForCtorParam("Estado",
                opciones => opciones.MapFrom(practica => practica.Estado.ToString()));
    }
}
