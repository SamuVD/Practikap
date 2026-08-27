using AutoMapper;
using Practikap.Application.DTOs.Usuarios;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>
/// Mapeos de la entidad <see cref="Usuario"/> hacia sus DTO de salida.
/// </summary>
/// <remarks>
/// El nombre del rol y el estado se aplanan a texto: el frontend consume
/// "Administrador" y "Activo", no identificadores ni valores numericos de
/// enumeracion.
///
/// El origen debe traer la navegacion Rol cargada. Todas las consultas de
/// UsuarioRepository que alimentan este mapeo usan Include(u => u.Rol).
/// </remarks>
public sealed class UsuarioMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public UsuarioMappingProfile()
    {
        CreateMap<Usuario, UsuarioResponse>()
            .ForCtorParam("Rol", opciones => opciones.MapFrom(usuario => usuario.Rol.Nombre))
            .ForCtorParam("Estado", opciones => opciones.MapFrom(usuario => usuario.Estado.ToString()));
    }
}