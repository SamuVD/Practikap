using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Practikap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MonitoriaSinEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_practicas_empresa_modalidad",
                table: "practicas");

            migrationBuilder.AddCheckConstraint(
                name: "chk_practicas_empresa_modalidad",
                table: "practicas",
                sql: "(modalidad IN ('Proyecto productivo','Monitoría') AND empresa_id IS NULL) OR (modalidad NOT IN ('Proyecto productivo','Monitoría') AND empresa_id IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_practicas_empresa_modalidad",
                table: "practicas");

            migrationBuilder.AddCheckConstraint(
                name: "chk_practicas_empresa_modalidad",
                table: "practicas",
                sql: "(modalidad = 'Proyecto productivo' AND empresa_id IS NULL) OR (modalidad <> 'Proyecto productivo' AND empresa_id IS NOT NULL)");
        }
    }
}
