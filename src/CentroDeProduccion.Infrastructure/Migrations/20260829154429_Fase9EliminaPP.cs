using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase9EliminaPP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantidadAcumuladaCompras",
                table: "Insumos");

            migrationBuilder.DropColumn(
                name: "PrecioPromedioPonderado",
                table: "Insumos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CantidadAcumuladaCompras",
                table: "Insumos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioPromedioPonderado",
                table: "Insumos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
