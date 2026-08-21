using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Rol"/> sobre la tabla roles.</summary>
public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
               .HasColumnName("id")
               .HasColumnType("int unsigned")
               .ValueGeneratedOnAdd();

        builder.Property(r => r.Nombre)
               .HasColumnName("nombre")
               .HasColumnType("varchar(30)")
               .IsRequired();

        builder.Property(r => r.Descripcion)
               .HasColumnName("descripcion")
               .HasColumnType("varchar(255)")
               .IsRequired();

        builder.HasIndex(r => r.Nombre)
               .IsUnique()
               .HasDatabaseName("uq_roles_nombre");

        // Los tres roles son un catalogo fijo que el Script_DDL.sql siembra.
        // Se declaran con objetos anonimos porque el constructor de Rol es
        // publico pero HasData requiere fijar el Id, que tiene setter privado.
        builder.HasData(
            new
            {
                Id = 1,
                Nombre = "Administrador",
                Descripcion = "Gobierna la plataforma, configura el Motor de Reglas y genera reportes globales."
            },
            new
            {
                Id = 2,
                Nombre = "Instructor",
                Descripcion = "Responsable pedagógico: asigna aprendices, registra seguimiento y califica."
            },
            new
            {
                Id = 3,
                Nombre = "Aprendiz",
                Descripcion = "Realiza la práctica: consulta su seguimiento y evalua al instructor."
            });
    }
}
