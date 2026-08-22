using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Observacion"/> sobre la tabla observaciones.</summary>
public class ObservacionConfiguration : IEntityTypeConfiguration<Observacion>
{
       /// <inheritdoc />
       public void Configure(EntityTypeBuilder<Observacion> builder)
       {
              builder.ToTable("observaciones");
              builder.HasKey(o => o.Id);

              builder.Property(o => o.Id)
                     .HasColumnName("id")
                     .HasColumnType("int unsigned")
                     .ValueGeneratedOnAdd();

              // Depende del seguimiento, no de la practica.
              builder.Property(o => o.SeguimientoId)
                     .HasColumnName("seguimiento_id")
                     .HasColumnType("int unsigned");

              builder.Property(o => o.Contenido)
                     .HasColumnName("contenido")
                     .HasColumnType("text")
                     .IsRequired();

              builder.Property(o => o.FechaRegistro)
                     .HasColumnName("fecha_registro")
                     .HasColumnType("datetime")
                     .HasDefaultValueSql("CURRENT_TIMESTAMP")
                     .ValueGeneratedOnAdd();

              builder.Property(o => o.Anulado)
                     .HasColumnName("anulado")
                     .HasColumnType("tinyint(1)")
                     .HasDefaultValue(false);

              builder.Property(o => o.AnuladoPor)
                     .HasColumnName("anulado_por")
                     .HasColumnType("int unsigned");

              builder.HasIndex(o => o.SeguimientoId)
                     .HasDatabaseName("idx_observaciones_seguimiento");

              builder.HasOne(o => o.Seguimiento)
                     .WithMany(s => s.Observaciones)
                     .HasForeignKey(o => o.SeguimientoId)
                     .HasConstraintName("fk_observaciones_seguimiento")
                     .OnDelete(DeleteBehavior.Restrict);

              builder.HasOne<Usuario>()
                     .WithMany()
                     .HasForeignKey(o => o.AnuladoPor)
                     .HasConstraintName("fk_observaciones_anulador")
                     .OnDelete(DeleteBehavior.Restrict);

              builder.HasIndex(o => o.AnuladoPor)
                     .HasDatabaseName("idx_observaciones_anulado_por");
       }
}
