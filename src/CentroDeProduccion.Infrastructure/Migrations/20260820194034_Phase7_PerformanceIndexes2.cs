using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_PerformanceIndexes2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Remitos_Fecha_Estado_BarId",
                table: "Remitos",
                columns: new[] { "Fecha", "Estado", "BarId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosHoras_Fecha_ProduccionId",
                table: "RegistrosHoras",
                columns: new[] { "Fecha", "ProduccionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Producciones_Fecha_RecetaId",
                table: "Producciones",
                columns: new[] { "Fecha", "RecetaId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_FechaCreacion_Estado_ProveedorId",
                table: "OrdenesCompra",
                columns: new[] { "FechaCreacion", "Estado", "ProveedorId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Remitos_Fecha_Estado_BarId",
                table: "Remitos");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosHoras_Fecha_ProduccionId",
                table: "RegistrosHoras");

            migrationBuilder.DropIndex(
                name: "IX_Producciones_Fecha_RecetaId",
                table: "Producciones");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesCompra_FechaCreacion_Estado_ProveedorId",
                table: "OrdenesCompra");
        }
    }
}
