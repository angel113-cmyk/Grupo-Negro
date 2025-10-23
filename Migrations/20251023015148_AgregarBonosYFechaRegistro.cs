using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grupo_negro.Migrations
{
    /// <inheritdoc />
    public partial class AgregarBonosYFechaRegistro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRegistro",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "BonosUsuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    Concepto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AsignadoPorId = table.Column<string>(type: "TEXT", nullable: false),
                    Aplicado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonosUsuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonosUsuarios_AspNetUsers_AsignadoPorId",
                        column: x => x.AsignadoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BonosUsuarios_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonosUsuarios_AsignadoPorId",
                table: "BonosUsuarios",
                column: "AsignadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_BonosUsuarios_UsuarioId",
                table: "BonosUsuarios",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonosUsuarios");

            migrationBuilder.DropColumn(
                name: "FechaRegistro",
                table: "AspNetUsers");
        }
    }
}
