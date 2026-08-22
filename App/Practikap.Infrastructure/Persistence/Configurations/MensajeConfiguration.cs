using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Mensaje"/> sobre la tabla mensajes.</summary>
/// <remarks>
/// Emisor y receptor son dos claves foraneas hacia usuarios sin propiedad de
/// navegacion. La dependencia obligatoria de practica_id es la que hace posible
/// el aislamiento de RN-13: no hay mensajeria fuera de una practica compartida.
/// </remarks>
public class MensajeConfiguration : IEntityTypeConfiguration<Mensaje>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Mensaje> builder)
    {
        builder.ToTable("mensajes");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasColumnName("id")
               .HasColumnType("int unsigned")
               .ValueGeneratedOnAdd();

        builder.Property(m => m.PracticaId)
               .HasColumnName("practica_id")
               .HasColumnType("int unsigned");

        builder.Property(m => m.EmisorId)
               .HasColumnName("emisor_id")
               .HasColumnType("int unsigned");

        builder.Property(m => m.ReceptorId)
               .HasColumnName("receptor_id")
               .HasColumnType("int unsigned");

        builder.Property(m => m.Contenido)
               .HasColumnName("contenido")
               .HasColumnType("text")
               .IsRequired();

        builder.Property(m => m.FechaEnvio)
               .HasColumnName("fecha_envio")
               .HasColumnType("datetime")
               .HasDefaultValueSql("CURRENT_TIMESTAMP")
               .ValueGeneratedOnAdd();

        builder.Property(m => m.Leido)
               .HasColumnName("leido")
               .HasColumnType("tinyint(1)")
               .HasDefaultValue(false);

        builder.HasIndex(m => m.PracticaId).HasDatabaseName("idx_mensajes_practica");
        builder.HasIndex(m => m.EmisorId).HasDatabaseName("idx_mensajes_emisor");
        builder.HasIndex(m => m.ReceptorId).HasDatabaseName("idx_mensajes_receptor");

        builder.HasOne(m => m.Practica)
               .WithMany()
               .HasForeignKey(m => m.PracticaId)
               .HasConstraintName("fk_mensajes_practica")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
               .WithMany()
               .HasForeignKey(m => m.EmisorId)
               .HasConstraintName("fk_mensajes_emisor")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
               .WithMany()
               .HasForeignKey(m => m.ReceptorId)
               .HasConstraintName("fk_mensajes_receptor")
               .OnDelete(DeleteBehavior.Restrict);
    }
}
