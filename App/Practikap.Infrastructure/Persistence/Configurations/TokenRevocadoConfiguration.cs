using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="TokenRevocado"/> sobre la tabla tokens_revocados.</summary>
public class TokenRevocadoConfiguration : IEntityTypeConfiguration<TokenRevocado>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TokenRevocado> builder)
    {
        builder.ToTable("tokens_revocados");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
               .HasColumnName("id")
               .HasColumnType("int unsigned")
               .ValueGeneratedOnAdd();

        builder.Property(t => t.UsuarioId)
               .HasColumnName("usuario_id")
               .HasColumnType("int unsigned");

        // Claim jti del JWT: longitud fija de 36 caracteres, no varchar.
        builder.Property(t => t.ReferenciaToken)
               .HasColumnName("referencia_token")
               .HasColumnType("char(36)")
               .IsRequired();

        builder.Property(t => t.Motivo)
               .HasColumnName("motivo")
               .HasConversion(ConvertidoresDeEnum.ConvertidorMotivoRevocacion)
               .HasColumnType("enum('Logout','Cambio de contraseña')")
               .IsRequired();

        builder.Property(t => t.FechaRevocacion)
               .HasColumnName("fecha_revocacion")
               .HasColumnType("datetime")
               .HasDefaultValueSql("CURRENT_TIMESTAMP")
               .ValueGeneratedOnAdd();

        builder.HasIndex(t => t.ReferenciaToken)
               .IsUnique()
               .HasDatabaseName("uq_tokens_revocados_referencia");

        builder.HasIndex(t => t.UsuarioId)
               .HasDatabaseName("idx_tokens_revocados_usuario");

        builder.HasOne(t => t.Usuario)
               .WithMany()
               .HasForeignKey(t => t.UsuarioId)
               .HasConstraintName("fk_tokens_revocados_usuario")
               .OnDelete(DeleteBehavior.Cascade);
    }
}
