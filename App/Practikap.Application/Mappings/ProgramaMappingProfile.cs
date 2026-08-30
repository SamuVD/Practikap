using AutoMapper;
using Practikap.Application.DTOs.Programas;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>
/// Mapeos de la entidad <see cref="Programa"/> hacia su DTO de salida.
/// </summary>
/// <remarks>
/// Mapeo por convencion: los tres parametros del record coinciden en nombre con
/// las propiedades de la entidad. La coleccion Fichas no se proyecta.
/// </remarks>
public sealed class ProgramaMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public ProgramaMappingProfile()
    {
        CreateMap<Programa, ProgramaResponse>();
    }
}
