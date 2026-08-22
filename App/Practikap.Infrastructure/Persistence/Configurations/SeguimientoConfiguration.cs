using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Seguimiento"/> sobre la tabla seguimientos.</summary>
public class SeguimientoConfiguration : IEntityTypeConfiguration<Seguimiento>
{
       /// <inheritdoc />
       public void Configure(EntityTypeBuilder<Seguimiento> builder)
       {
              builder.ToTable("seguimientos");
              builder.HasKey(s => s.Id);

              builder.Property(s => s.Id)
                     .HasColumnName("id")
                     .HasColumnType("int unsigned")
                     .ValueGeneratedOnAdd();

              builder.Property(s => s.PracticaId)
                     .HasColumnName("practica_id")
                     .HasColumnType("int unsigned");

              builder.Property(s => s.Avance)
                     .HasColumnName("avance")
                     .HasColumnType("text")
                     .IsRequired();

              builder.Property(s => s.Etapa)
                     .HasColumnName("etapa")
                     .HasColumnType("varchar(100)")
                     .IsRequired();

              // RN-11: la marca de tiempo la pone el servidor de base de datos,
              // nunca el cliente ni el dominio.
              builder.Property(s => s.FechaRegistro)
                     .HasColumnName("fecha_registro")
                     .HasColumnType("datetime")
                     .HasDefaultValueSql("CURRENT_TIMESTAMP")
                     .ValueGeneratedOnAdd();

              builder.Property(s => s.Anulado)
                     .HasColumnName("anulado")
                     .HasColumnType("tinyint(1)")
                     .HasDefaultValue(false);

              builder.Property(s => s.AnuladoPor)
                     .HasColumnName("anulado_por")
                     .HasColumnType("int unsigned");

              builder.HasIndex(s => s.PracticaId)
                     .HasDatabaseName("idx_seguimientos_practica");

              builder.HasOne(s => s.Practica)
                     .WithMany(p => p.Seguimientos)
                     .HasForeignKey(s => s.PracticaId)
                     .HasConstraintName("fk_seguimientos_practica")
                     .OnDelete(DeleteBehavior.Restrict);

              builder.HasOne<Usuario>()
                     .WithMany()
                     .HasForeignKey(s => s.AnuladoPor)
                     .HasConstraintName("fk_seguimientos_anulador")
                     .OnDelete(DeleteBehavior.Restrict);

              builder.Navigation(s => s.Observaciones)
                     .HasField("_observaciones")
                     .UsePropertyAccessMode(PropertyAccessMode.Field);

              builder.HasIndex(s => s.AnuladoPor)
                     .HasDatabaseName("idx_seguimientos_anulado_por");
       }
}
