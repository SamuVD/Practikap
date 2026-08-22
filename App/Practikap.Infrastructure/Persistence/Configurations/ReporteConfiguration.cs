using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Reporte"/> sobre la tabla reportes.</summary>
/// <remarks>
/// La coleccion Practicas es una navegacion de salto: la tabla puente
/// reporte_practica es una relacion pura que el Doc_Arquitectura 5.2 deja sin
/// clase de dominio. Sin la declaracion UsingEntity de mas abajo, EF Core
/// inventaria por su cuenta una tabla PracticaReporte con columnas PracticasId
/// y ReportesId, que no es lo que declara el Script_DDL.sql.
/// </remarks>
public class ReporteConfiguration : IEntityTypeConfiguration<Reporte>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Reporte> builder)
    {
        builder.ToTable("reportes");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
               .HasColumnName("id")
               .HasColumnType("int unsigned")
               .ValueGeneratedOnAdd();

        builder.Property(r => r.Tipo)
               .HasColumnName("tipo")
               .HasConversion(ConvertidoresDeEnum.ConvertidorTipoReporte)
               .HasColumnType("enum('Individual','Grupal')")
               .IsRequired();

        // El Dominio guarda los criterios ya serializados como texto JSON: la
        // forma tipada es FiltroReporte y su serializacion corresponde a la
        // capa de Aplicacion, de modo que aqui no hace falta convertidor.
        builder.Property(r => r.Filtros)
               .HasColumnName("filtros")
               .HasColumnType("json")
               .IsRequired();

        builder.Property(r => r.GeneradoPor)
               .HasColumnName("generado_por")
               .HasColumnType("int unsigned");

        builder.Property(r => r.FechaGeneracion)
               .HasColumnName("fecha_generacion")
               .HasColumnType("datetime")
               .HasDefaultValueSql("CURRENT_TIMESTAMP")
               .ValueGeneratedOnAdd();

        builder.HasIndex(r => r.GeneradoPor)
               .HasDatabaseName("idx_reportes_generador");

        builder.HasOne<Usuario>()
               .WithMany()
               .HasForeignKey(r => r.GeneradoPor)
               .HasConstraintName("fk_reportes_generador")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Practicas)
               .WithMany()
               .UsingEntity(
                   "reporte_practica",
                   haciaPractica => haciaPractica
                       .HasOne(typeof(Practica))
                       .WithMany()
                       .HasForeignKey("practica_id")
                       .HasConstraintName("fk_reporte_practica_practica")
                       .OnDelete(DeleteBehavior.Restrict),
                   haciaReporte => haciaReporte
                       .HasOne(typeof(Reporte))
                       .WithMany()
                       .HasForeignKey("reporte_id")
                       .HasConstraintName("fk_reporte_practica_reporte")
                       .OnDelete(DeleteBehavior.Cascade),
                   puente =>
                   {
                       puente.ToTable("reporte_practica");
                       puente.Property<int>("reporte_id").HasColumnType("int unsigned");
                       puente.Property<int>("practica_id").HasColumnType("int unsigned");
                       puente.HasKey("reporte_id", "practica_id");
                       puente.HasIndex("practica_id")
                             .HasDatabaseName("idx_reporte_practica_practica");
                   });

        builder.Navigation(r => r.Practicas)
               .HasField("_practicas")
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
