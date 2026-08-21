using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Configuracion"/> sobre la tabla configuracion.</summary>
public class ConfiguracionConfiguration : IEntityTypeConfiguration<Configuracion>
{
       /// <inheritdoc />
       public void Configure(EntityTypeBuilder<Configuracion> builder)
       {
              builder.ToTable("configuracion");
              builder.HasKey(c => c.Id);

              builder.Property(c => c.Id)
                     .HasColumnName("id")
                     .HasColumnType("int unsigned")
                     .ValueGeneratedOnAdd();

              builder.Property(c => c.Clave)
                     .HasColumnName("clave")
                     .HasColumnType("varchar(100)")
                     .IsRequired();

              builder.Property(c => c.Valor)
                     .HasColumnName("valor")
                     .HasColumnType("varchar(255)")
                     .IsRequired();

              builder.Property(c => c.Descripcion)
                     .HasColumnName("descripcion")
                     .HasColumnType("varchar(255)");

              builder.Property(c => c.ActualizadoPor)
                     .HasColumnName("actualizado_por")
                     .HasColumnType("int unsigned");

              builder.Property(c => c.FechaActualizacion)
                     .HasColumnName("fecha_actualizacion")
                     .HasColumnType("datetime")
                     .HasDefaultValueSql("CURRENT_TIMESTAMP")
                     .ValueGeneratedOnAddOrUpdate();

              builder.HasIndex(c => c.Clave)
                     .IsUnique()
                     .HasDatabaseName("uq_configuracion_clave");

              builder.HasOne(c => c.Actualizador)
                     .WithMany()
                     .HasForeignKey(c => c.ActualizadoPor)
                     .HasConstraintName("fk_configuracion_actualizador")
                     .OnDelete(DeleteBehavior.Restrict);

              builder.HasIndex(c => c.ActualizadoPor)
                     .HasDatabaseName("idx_configuracion_actualizado_por");
       }
}
