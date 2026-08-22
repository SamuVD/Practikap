using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="RegistroAuditoria"/> sobre la tabla auditoria.</summary>
/// <remarks>
/// ADR-06 fija que esta entidad no tiene propiedades de navegacion. La clave
/// foranea fk_auditoria_usuario del Script_DDL.sql se conserva declarandola sin
/// navegacion, de modo que el esquema no pierde la restriccion y el Dominio no
/// gana una propiedad que el ADR excluye.
/// </remarks>
public class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("auditoria");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasColumnName("id")
               .HasColumnType("int unsigned")
               .ValueGeneratedOnAdd();

        builder.Property(a => a.UsuarioId)
               .HasColumnName("usuario_id")
               .HasColumnType("int unsigned");

        // Unica enumeracion que no mapea a una columna ENUM: el DDL declara
        // entidad_afectada como VARCHAR(50) por ser una referencia polimorfica.
        builder.Property(a => a.EntidadAfectada)
               .HasColumnName("entidad_afectada")
               .HasConversion(ConvertidoresDeEnum.ConvertidorEntidadAuditada)
               .HasColumnType("varchar(50)")
               .IsRequired();

        builder.Property(a => a.EntidadId)
               .HasColumnName("entidad_id")
               .HasColumnType("int unsigned");

        builder.Property(a => a.Accion)
               .HasColumnName("accion")
               .HasConversion(ConvertidoresDeEnum.ConvertidorAccionAuditoria)
               .HasColumnType("enum('Anulacion','Retroceso_estado','Cambio_rol','Reasignacion','Configuracion_regla','Otro')")
               .IsRequired();

        builder.Property(a => a.Detalle)
               .HasColumnName("detalle")
               .HasColumnType("varchar(255)");

        builder.Property(a => a.FechaRegistro)
               .HasColumnName("fecha_registro")
               .HasColumnType("datetime")
               .HasDefaultValueSql("CURRENT_TIMESTAMP")
               .ValueGeneratedOnAdd();

        builder.HasIndex(a => a.UsuarioId)
               .HasDatabaseName("idx_auditoria_usuario");

        builder.HasIndex(a => new { a.EntidadAfectada, a.EntidadId })
               .HasDatabaseName("idx_auditoria_entidad");

        builder.HasOne<Usuario>()
               .WithMany()
               .HasForeignKey(a => a.UsuarioId)
               .HasConstraintName("fk_auditoria_usuario")
               .OnDelete(DeleteBehavior.Restrict);
    }
}
