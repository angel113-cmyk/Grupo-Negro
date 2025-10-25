using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grupo_negro.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposMercadoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoCollectionId",
                table: "Transacciones",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoPaymentId",
                table: "Transacciones",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoPreferenceId",
                table: "Transacciones",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoStatus",
                table: "Transacciones",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MercadoPagoCollectionId",
                table: "Transacciones");

            migrationBuilder.DropColumn(
                name: "MercadoPagoPaymentId",
                table: "Transacciones");

            migrationBuilder.DropColumn(
                name: "MercadoPagoPreferenceId",
                table: "Transacciones");

            migrationBuilder.DropColumn(
                name: "MercadoPagoStatus",
                table: "Transacciones");
        }
    }
}
