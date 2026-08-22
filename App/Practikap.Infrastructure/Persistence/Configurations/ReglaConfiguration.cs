using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Regla"/> sobre la tabla reglas.</summary>
public class ReglaConfiguration : IEntityTypeConfiguration<Regla>
{
       /// <inheritdoc />
       public void Configure(EntityTypeBuilder<Regla> builder)
       {
              builder.ToTable("reglas");
              builder.HasKey(r => r.Id);

              builder.Property(r => r.Id)
                     .HasColumnName("id")
                     .HasColumnType("int unsigned")
                     .ValueGeneratedOnAdd();

              builder.Property(r => r.Nombre)
                     .HasColumnName("nombre")
                     .HasColumnType("varchar(150)")
                     .IsRequired();

              builder.Property(r => r.CampoEvaluado)
                     .HasColumnName("campo_evaluado")
                     .HasColumnType("varchar(100)")
                     .IsRequired();

              builder.Property(r => r.Operador)
                     .HasColumnName("operador")
                     .HasConversion(ConvertidoresDeEnum.ConvertidorOperadorComparacion)
                     .HasColumnType("enum('>','>=','<','<=','=','!=')")
                     .IsRequired();

              builder.Property(r => r.ValorCondicion)
                     .HasColumnName("valor_condicion")
                     .HasColumnType("decimal(6,2)");

              builder.Property(r => r.Umbral)
                     .HasColumnName("umbral")
                     .HasColumnType("decimal(6,2)");

              builder.Property(r => r.AccionResultante)
                     .HasColumnName("accion_resultante")
                     .HasColumnType("varchar(255)")
                     .IsRequired();

              // Orden de evaluacion determinista de RN-07.
              builder.Property(r => r.Prioridad)
                     .HasColumnName("prioridad")
                     .HasColumnType("int unsigned")
                     .HasDefaultValue(0);

              builder.Property(r => r.Activa)
                     .HasColumnName("activa")
                     .HasColumnType("tinyint(1)")
                     .HasDefaultValue(true);

              builder.Property(r => r.CreadoPor)
                     .HasColumnName("creado_por")
                     .HasColumnType("int unsigned");

              builder.Property(r => r.FechaCreacion)
                     .HasColumnName("fecha_creacion")
                     .HasColumnType("datetime")
                     .HasDefaultValueSql("CURRENT_TIMESTAMP")
                     .ValueGeneratedOnAdd();

              builder.Property(r => r.FechaActualizacion)
                     .HasColumnName("fecha_actualizacion")
                     .HasColumnType("datetime")
                     .HasDefaultValueSql("CURRENT_TIMESTAMP")
                     .ValueGeneratedOnAddOrUpdate();

              // Indice compuesto que sostiene ListarActivasOrdenadasAsync (RN-07).
              builder.HasIndex(r => new { r.Activa, r.Prioridad })
                     .HasDatabaseName("idx_reglas_activa_prioridad");

              builder.HasOne(r => r.Creador)
                     .WithMany()
                     .HasForeignKey(r => r.CreadoPor)
                     .HasConstraintName("fk_reglas_creador")
                     .OnDelete(DeleteBehavior.Restrict);

              builder.HasIndex(r => r.CreadoPor)
                     .HasDatabaseName("idx_reglas_creado_por");
       }
}
