using AutoMapper;
using Practikap.Application.DTOs.Roles;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>Mapeos de la entidad <see cref="Rol"/> hacia sus DTO de salida.</summary>
public sealed class RolMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public RolMappingProfile()
    {
        CreateMap<Rol, RolResponse>();
    }
}