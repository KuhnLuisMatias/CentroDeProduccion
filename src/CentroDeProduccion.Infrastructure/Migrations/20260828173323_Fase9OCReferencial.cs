using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase9OCReferencial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // OC is now REFERENTIAL ONLY: fold legacy Confirmada/Recibida/ParcialRecibida states
            // (3, 4, 5) into Enviada (2) before dropping the reception-related columns.
            migrationBuilder.Sql("UPDATE [OrdenesCompra] SET [Estado] = 2 WHERE [Estado] IN (3, 4, 5);");

            migrationBuilder.DropColumn(
                name: "FechaConfirmacion",
                table: "OrdenesCompra");

            migrationBuilder.DropColumn(
                name: "CantidadRecibida",
                table: "OrdenCompraItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(                name: "FechaConfirmacion",
                table: "OrdenesCompra",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CantidadRecibida",
                table: "OrdenCompraItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
