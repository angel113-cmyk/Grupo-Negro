using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grupo_negro.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSistemaPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transacciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    MetodoPago = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReferenciaPago = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NumeroOperacion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaCompletado = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PayPalOrderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PayPalPaymentId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ApuestaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transacciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transacciones_Apuestas_ApuestaId",
                        column: x => x.ApuestaId,
                        principalTable: "Apuestas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transacciones_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_ApuestaId",
                table: "Transacciones",
                column: "ApuestaId");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_UsuarioId",
                table: "Transacciones",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transacciones");
        }
    }
}
