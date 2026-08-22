using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Ficha"/> sobre la tabla fichas.</summary>
public class FichaConfiguration : IEntityTypeConfiguration<Ficha>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Ficha> builder)
    {
        builder.ToTable("fichas");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
               .HasColumnName("id")
               .HasColumnType("int unsigned")
               .ValueGeneratedOnAdd();

        builder.Property(f => f.NumeroFicha)
               .HasColumnName("numero_ficha")
               .HasColumnType("varchar(20)")
               .IsRequired();

        builder.Property(f => f.ProgramaId)
               .HasColumnName("programa_id")
               .HasColumnType("int unsigned");

        builder.HasIndex(f => f.NumeroFicha)
               .IsUnique()
               .HasDatabaseName("uq_fichas_numero");

        builder.HasIndex(f => f.ProgramaId)
               .HasDatabaseName("idx_fichas_programa");

        builder.HasOne(f => f.Programa)
               .WithMany(p => p.Fichas)
               .HasForeignKey(f => f.ProgramaId)
               .HasConstraintName("fk_fichas_programa")
               .OnDelete(DeleteBehavior.Restrict);
    }
}
