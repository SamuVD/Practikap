using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Programa"/> sobre la tabla programas.</summary>
public class ProgramaConfiguration : IEntityTypeConfiguration<Programa>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Programa> builder)
    {
        builder.ToTable("programas");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasColumnName("id")
               .HasColumnType("int unsigned")
               .ValueGeneratedOnAdd();

        builder.Property(p => p.Nombre)
               .HasColumnName("nombre")
               .HasColumnType("varchar(150)")
               .IsRequired();

        builder.Property(p => p.Descripcion)
               .HasColumnName("descripcion")
               .HasColumnType("varchar(255)");

        builder.HasIndex(p => p.Nombre)
               .IsUnique()
               .HasDatabaseName("uq_programas_nombre");

        // La coleccion se expone como IReadOnlyCollection sobre el campo
        // privado _fichas: EF debe escribir en el campo, no en la propiedad.
        // La relacion en si se declara en FichaConfiguration, del lado que
        // posee la clave foranea.
        builder.Navigation(p => p.Fichas)
               .HasField("_fichas")
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
