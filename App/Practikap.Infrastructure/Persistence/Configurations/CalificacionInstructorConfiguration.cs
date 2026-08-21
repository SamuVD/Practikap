using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="CalificacionInstructor"/> sobre la tabla calificaciones_instructor.</summary>
/// <remarks>
/// Es una de las dos tablas separadas que CU-05 y HU-07 exigen para la
/// calificacion bidireccional. El CHECK de rango replica el del
/// Script_DDL.sql y refleja las constantes ValorMinimo y ValorMaximo de la
/// entidad (RN-10).
/// </remarks>
public class CalificacionInstructorConfiguration : IEntityTypeConfiguration<CalificacionInstructor>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CalificacionInstructor> builder)
    {
        builder.ToTable("calificaciones_instructor", t =>
            t.HasCheckConstraint(
                "chk_calificaciones_instructor_valor",
                "valor >= 0.0 AND valor <= 5.0"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasColumnName("id")
               .HasColumnType("int unsigned")
               .ValueGeneratedOnAdd();

        builder.Property(c => c.PracticaId)
               .HasColumnName("practica_id")
               .HasColumnType("int unsigned");

        builder.Property(c => c.Valor)
               .HasColumnName("valor")
               .HasColumnType("decimal(3,1)");

        builder.Property(c => c.Comentario)
               .HasColumnName("comentario")
               .HasColumnType("text");

        builder.Property(c => c.FechaRegistro)
               .HasColumnName("fecha_registro")
               .HasColumnType("datetime")
               .HasDefaultValueSql("CURRENT_TIMESTAMP")
               .ValueGeneratedOnAdd();

        builder.Property(c => c.Anulado)
               .HasColumnName("anulado")
               .HasColumnType("tinyint(1)")
               .HasDefaultValue(false);

        builder.Property(c => c.AnuladoPor)
               .HasColumnName("anulado_por")
               .HasColumnType("int unsigned");

        builder.Ignore(c => c.EsVigente);

        builder.HasIndex(c => c.PracticaId)
               .HasDatabaseName("idx_calificaciones_instructor_practica");

        builder.HasOne(c => c.Practica)
               .WithMany()
               .HasForeignKey(c => c.PracticaId)
               .HasConstraintName("fk_calificaciones_instructor_practica")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
               .WithMany()
               .HasForeignKey(c => c.AnuladoPor)
               .HasConstraintName("fk_calificaciones_instructor_anulador")
               .OnDelete(DeleteBehavior.Restrict);
    }
}
