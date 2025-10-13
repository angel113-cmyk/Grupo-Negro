using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grupo_negro.Migrations
{
    /// <inheritdoc />
    public partial class AgregarApuestasCombinadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApuestasCombinadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    MontoApostado = table.Column<decimal>(type: "TEXT", nullable: false),
                    CuotaTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    PosibleGanancia = table.Column<decimal>(type: "TEXT", nullable: false),
                    FechaApuesta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApuestasCombinadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApuestasCombinadas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetallesApuestasCombinadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApuestaCombinadadId = table.Column<int>(type: "INTEGER", nullable: false),
                    PartidoId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoApuesta = table.Column<int>(type: "INTEGER", nullable: false),
                    CuotaSeleccionada = table.Column<decimal>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesApuestasCombinadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesApuestasCombinadas_ApuestasCombinadas_ApuestaCombinadadId",
                        column: x => x.ApuestaCombinadadId,
                        principalTable: "ApuestasCombinadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesApuestasCombinadas_Partidos_PartidoId",
                        column: x => x.PartidoId,
                        principalTable: "Partidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApuestasCombinadas_UsuarioId",
                table: "ApuestasCombinadas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesApuestasCombinadas_ApuestaCombinadadId",
                table: "DetallesApuestasCombinadas",
                column: "ApuestaCombinadadId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesApuestasCombinadas_PartidoId",
                table: "DetallesApuestasCombinadas",
                column: "PartidoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesApuestasCombinadas");

            migrationBuilder.DropTable(
                name: "ApuestasCombinadas");
        }
    }
}
