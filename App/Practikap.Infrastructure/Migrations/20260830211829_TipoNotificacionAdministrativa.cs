using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Practikap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TipoNotificacionAdministrativa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "tipo",
                table: "notificaciones",
                type: "enum('Calificacion','Mensaje','Observacion','Riesgo','Administrativa')",
                nullable: false,
                collation: "utf8mb4_0900_ai_ci",
                oldClrType: typeof(string),
                oldType: "enum('Calificacion','Mensaje','Observacion','Riesgo')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_0900_ai_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "tipo",
                table: "notificaciones",
                type: "enum('Calificacion','Mensaje','Observacion','Riesgo')",
                nullable: false,
                collation: "utf8mb4_0900_ai_ci",
                oldClrType: typeof(string),
                oldType: "enum('Calificacion','Mensaje','Observacion','Riesgo','Administrativa')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_0900_ai_ci");
        }
    }
}
