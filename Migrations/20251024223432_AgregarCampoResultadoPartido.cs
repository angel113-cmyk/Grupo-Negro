using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grupo_negro.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCampoResultadoPartido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Resultado",
                table: "Partidos",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Resultado",
                table: "Partidos");
        }
    }
}
