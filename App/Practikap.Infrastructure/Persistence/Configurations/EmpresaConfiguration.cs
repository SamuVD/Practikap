using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Empresa"/> sobre la tabla empresas.</summary>
public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresas");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("id")
               .HasColumnType("int unsigned")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.RazonSocial)
               .HasColumnName("razon_social")
               .HasColumnType("varchar(200)")
               .IsRequired();

        builder.Property(e => e.Nit)
               .HasColumnName("nit")
               .HasColumnType("varchar(20)")
               .IsRequired();

        builder.Property(e => e.JefeInmediatoNombre)
               .HasColumnName("jefe_inmediato_nombre")
               .HasColumnType("varchar(150)");

        builder.Property(e => e.JefeInmediatoCorreo)
               .HasColumnName("jefe_inmediato_correo")
               .HasColumnType("varchar(180)");

        builder.Property(e => e.JefeInmediatoTelefono)
               .HasColumnName("jefe_inmediato_telefono")
               .HasColumnType("varchar(20)");

        builder.HasIndex(e => e.Nit)
               .IsUnique()
               .HasDatabaseName("uq_empresas_nit");
    }
}
