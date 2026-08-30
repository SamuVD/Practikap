using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Notificacion"/> sobre la tabla notificaciones.</summary>
/// <remarks>
/// regla_id es opcional y solo se llena cuando la alerta la disparo el Motor de
/// Reglas: es el rastro que exige RN-09. El destinatario se borra en cascada
/// junto con el usuario, siguiendo el Script_DDL.sql.
/// </remarks>
public class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.ToTable("notificaciones");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
               .HasColumnName("id")
               .HasColumnType("int unsigned")
               .ValueGeneratedOnAdd();

        builder.Property(n => n.UsuarioId)
               .HasColumnName("usuario_id")
               .HasColumnType("int unsigned");

        builder.Property(n => n.ReglaId)
               .HasColumnName("regla_id")
               .HasColumnType("int unsigned");

        builder.Property(n => n.Tipo)
               .HasColumnName("tipo")
               .HasConversion(ConvertidoresDeEnum.ConvertidorTipoNotificacion)
               .HasColumnType("enum('Calificacion','Mensaje','Observacion','Riesgo','Administrativa')")
               .IsRequired();

        builder.Property(n => n.Contenido)
               .HasColumnName("contenido")
               .HasColumnType("varchar(255)")
               .IsRequired();

        builder.Property(n => n.Leida)
               .HasColumnName("leida")
               .HasColumnType("tinyint(1)")
               .HasDefaultValue(false);

        builder.Property(n => n.FechaGeneracion)
               .HasColumnName("fecha_generacion")
               .HasColumnType("datetime")
               .HasDefaultValueSql("CURRENT_TIMESTAMP")
               .ValueGeneratedOnAdd();

        builder.HasIndex(n => n.UsuarioId).HasDatabaseName("idx_notificaciones_usuario");
        builder.HasIndex(n => n.ReglaId).HasDatabaseName("idx_notificaciones_regla");

        builder.HasOne<Usuario>()
               .WithMany()
               .HasForeignKey(n => n.UsuarioId)
               .HasConstraintName("fk_notificaciones_usuario")
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Regla)
               .WithMany()
               .HasForeignKey(n => n.ReglaId)
               .HasConstraintName("fk_notificaciones_regla")
               .OnDelete(DeleteBehavior.Restrict);
    }
}
