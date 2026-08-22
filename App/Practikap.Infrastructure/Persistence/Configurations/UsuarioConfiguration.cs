using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Usuario"/> sobre la tabla usuarios.</summary>
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
               .HasColumnName("id")
               .HasColumnType("int unsigned")
               .ValueGeneratedOnAdd();

        builder.Property(u => u.RolId)
               .HasColumnName("rol_id")
               .HasColumnType("int unsigned");

        builder.Property(u => u.Correo)
               .HasColumnName("correo")
               .HasColumnType("varchar(180)")
               .IsRequired();

        builder.Property(u => u.ContrasenaHash)
               .HasColumnName("contrasena_hash")
               .HasColumnType("varchar(255)")
               .IsRequired();

        builder.Property(u => u.Nombre)
               .HasColumnName("nombre")
               .HasColumnType("varchar(150)")
               .IsRequired();

        builder.Property(u => u.Apellido)
               .HasColumnName("apellido")
               .HasColumnType("varchar(150)")
               .IsRequired();

        builder.Property(u => u.Telefono)
               .HasColumnName("telefono")
               .HasColumnType("varchar(20)");

        builder.Property(u => u.Estado)
               .HasColumnName("estado")
               .HasConversion(ConvertidoresDeEnum.ConvertidorEstadoUsuario)
               .HasColumnType("enum('Activo','Inactivo')")
               .HasDefaultValue(Practikap.Domain.Enums.EstadoUsuario.Activo)
               .IsRequired();

        builder.Property(u => u.FechaCreacion)
               .HasColumnName("fecha_creacion")
               .HasColumnType("datetime")
               .HasDefaultValueSql("CURRENT_TIMESTAMP")
               .ValueGeneratedOnAdd();

        builder.Property(u => u.FechaActualizacion)
               .HasColumnName("fecha_actualizacion")
               .HasColumnType("datetime")
               .HasDefaultValueSql("CURRENT_TIMESTAMP")
               .ValueGeneratedOnAddOrUpdate();

        // Propiedades calculadas del Dominio: no son columnas.
        builder.Ignore(u => u.NombreCompleto);
        builder.Ignore(u => u.EstaActivo);

        builder.HasIndex(u => u.Correo)
               .IsUnique()
               .HasDatabaseName("uq_usuarios_correo");

        builder.HasIndex(u => u.RolId)
               .HasDatabaseName("idx_usuarios_rol");

        builder.HasOne(u => u.Rol)
               .WithMany()
               .HasForeignKey(u => u.RolId)
               .HasConstraintName("fk_usuarios_rol")
               .OnDelete(DeleteBehavior.Restrict);
    }
}
