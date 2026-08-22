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

              // Claim jti del JWT: 36 caracteres (formato GUID). El Script_DDL.sql
              // declara CHAR(36), pero HasMaxLength(36)+IsFixedLength() (y tambien
              // HasColumnType("char(36)") directo) disparan un NullReferenceException
              // de Pomelo 9.0.0 en RelationalTypeMappingSource.FindCollectionMapping
              // durante la generacion de SQL (dotnet ef migrations script/remove),
              // aparentemente porque una columna de longitud fija 36 se confunde con
              // un candidato a GUID en la cache interna de mapeo de tipos. No hay
              // reporte identico en el repositorio de Pomelo a la fecha; el patron
              // coincide con los issues #1823 y #1934 (byte[]/Guid + tipo de columna
              // explicito). Se acepta VARCHAR(36) como desviacion documentada frente
              // al DDL: mismo contenido posible (36 caracteres), sin impacto practico
              // para una unica columna de este tamano. Anotado para Doc_Scaffolding.
              builder.Property(t => t.ReferenciaToken)
                     .HasColumnName("referencia_token")
                     .HasMaxLength(36)
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
