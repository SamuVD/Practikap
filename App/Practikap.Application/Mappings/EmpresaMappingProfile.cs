using AutoMapper;
using Practikap.Application.DTOs.Empresas;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>
/// Mapeos de la entidad <see cref="Empresa"/> hacia su DTO de salida.
/// </summary>
/// <remarks>
/// Mapeo por convencion: los seis parametros del record coinciden en nombre con
/// las propiedades de la entidad.
/// </remarks>
public sealed class EmpresaMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public EmpresaMappingProfile()
    {
        CreateMap<Empresa, EmpresaResponse>();
    }
}
