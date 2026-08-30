using AutoMapper;
using Practikap.Application.DTOs.Fichas;
using Practikap.Domain.Entities;

namespace Practikap.Application.Mappings;

/// <summary>
/// Mapeos de la entidad <see cref="Ficha"/> hacia su DTO de salida.
/// </summary>
/// <remarks>
/// El origen debe traer la navegacion Programa cargada. Todas las consultas de
/// FichaRepository usan Include(f =&gt; f.Programa), y CrearFichaUseCase relee con
/// ObtenerPorIdAsync tras confirmar por la misma razon.
/// </remarks>
public sealed class FichaMappingProfile : Profile
{
    /// <summary>Declara los mapeos del perfil.</summary>
    public FichaMappingProfile()
    {
        CreateMap<Ficha, FichaResponse>()
            .ForCtorParam("Programa",
                opciones => opciones.MapFrom(ficha => ficha.Programa.Nombre));
    }
}
