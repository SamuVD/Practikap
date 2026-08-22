using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Practica"/> sobre la tabla practicas.</summary>
/// <remarks>
/// El instructor y el aprendiz se declaran como claves foraneas sin propiedad
/// de navegacion, porque la entidad del Dominio solo guarda sus identificadores.
/// Las dos restricciones CHECK del Script_DDL.sql se replican aqui para que la
/// base de datos siga siendo la ultima linea de defensa de RN-04 y de la
/// coherencia entre modalidad y empresa, aunque la entidad ya las valide.
/// </remarks>
public class PracticaConfiguration : IEntityTypeConfiguration<Practica>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Practica> builder)
    {
        builder.ToTable("practicas", t =>
        {
            t.HasCheckConstraint(
                "chk_practicas_fechas",
                "fecha_fin IS NULL OR fecha_fin >= fecha_inicio");

            t.HasCheckConstraint(
                "chk_practicas_empresa_modalidad",
                "(modalidad = 'Proyecto productivo' AND empresa_id IS NULL) " +
                "OR (modalidad <> 'Proyecto productivo' AND empresa_id IS NOT NULL)");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasColumnName("id")
               .HasColumnType("int unsigned")
               .ValueGeneratedOnAdd();

        builder.Property(p => p.FichaId)
               .HasColumnName("ficha_id")
               .HasColumnType("int unsigned");

        builder.Property(p => p.EmpresaId)
               .HasColumnName("empresa_id")
               .HasColumnType("int unsigned");

        builder.Property(p => p.InstructorId)
               .HasColumnName("instructor_id")
               .HasColumnType("int unsigned");

        builder.Property(p => p.AprendizId)
               .HasColumnName("aprendiz_id")
               .HasColumnType("int unsigned");

        builder.Property(p => p.Modalidad)
               .HasColumnName("modalidad")
               .HasConversion(ConvertidoresDeEnum.ConvertidorModalidadPractica)
               .HasColumnType("enum('Contrato de aprendizaje','Pasantía','Proyecto productivo','Monitoría')")
               .HasDefaultValue(Practikap.Domain.Enums.ModalidadPractica.ContratoDeAprendizaje)
               .IsRequired();

        builder.Property(p => p.Estado)
               .HasColumnName("estado")
               .HasConversion(ConvertidoresDeEnum.ConvertidorEstadoPractica)
               .HasColumnType("enum('Pendiente','En curso','Finalizada','En riesgo')")
               .HasDefaultValue(Practikap.Domain.Enums.EstadoPractica.Pendiente)
               .IsRequired();

        // DateOnly sobre columna DATE: Pomelo 9 lo mapea de forma nativa.
        builder.Property(p => p.FechaInicio)
               .HasColumnName("fecha_inicio")
               .HasColumnType("date");

        builder.Property(p => p.FechaFin)
               .HasColumnName("fecha_fin")
               .HasColumnType("date");

        builder.Property(p => p.FechaCreacion)
               .HasColumnName("fecha_creacion")
               .HasColumnType("datetime")
               .HasDefaultValueSql("CURRENT_TIMESTAMP")
               .ValueGeneratedOnAdd();

        // Propiedad calculada del Dominio: no es columna.
        builder.Ignore(p => p.EstaActiva);

        builder.HasIndex(p => p.FichaId).HasDatabaseName("idx_practicas_ficha");
        builder.HasIndex(p => p.EmpresaId).HasDatabaseName("idx_practicas_empresa");
        builder.HasIndex(p => p.InstructorId).HasDatabaseName("idx_practicas_instructor");
        builder.HasIndex(p => p.AprendizId).HasDatabaseName("idx_practicas_aprendiz");
        builder.HasIndex(p => p.Estado).HasDatabaseName("idx_practicas_estado");

        builder.HasOne(p => p.Ficha)
               .WithMany()
               .HasForeignKey(p => p.FichaId)
               .HasConstraintName("fk_practicas_ficha")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Empresa)
               .WithMany()
               .HasForeignKey(p => p.EmpresaId)
               .HasConstraintName("fk_practicas_empresa")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
               .WithMany()
               .HasForeignKey(p => p.InstructorId)
               .HasConstraintName("fk_practicas_instructor")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
               .WithMany()
               .HasForeignKey(p => p.AprendizId)
               .HasConstraintName("fk_practicas_aprendiz")
               .OnDelete(DeleteBehavior.Restrict);

        // La relacion con Seguimiento se declara en SeguimientoConfiguration.
        builder.Navigation(p => p.Seguimientos)
               .HasField("_seguimientos")
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
