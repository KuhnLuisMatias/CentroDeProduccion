using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PagoChequeDatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChequeBanco",
                table: "PagosAProveedoresMetodos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChequeFechaPago",
                table: "PagosAProveedoresMetodos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChequeNumero",
                table: "PagosAProveedoresMetodos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChequeBanco",
                table: "PagosAProveedoresMetodos");

            migrationBuilder.DropColumn(
                name: "ChequeFechaPago",
                table: "PagosAProveedoresMetodos");

            migrationBuilder.DropColumn(
                name: "ChequeNumero",
                table: "PagosAProveedoresMetodos");
        }
    }
}
