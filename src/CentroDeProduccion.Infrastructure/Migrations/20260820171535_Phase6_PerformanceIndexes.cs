using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_PerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_Fecha_Tipo_InsumoId",
                table: "MovimientosStock",
                columns: new[] { "Fecha", "Tipo", "InsumoId" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_Fecha_Tipo_ProductoTerminadoId",
                table: "MovimientosStock",
                columns: new[] { "Fecha", "Tipo", "ProductoTerminadoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Producciones_Fecha_Estado",
                table: "Producciones",
                columns: new[] { "Fecha", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosHoras_Fecha_EmpleadoId",
                table: "RegistrosHoras",
                columns: new[] { "Fecha", "EmpleadoId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosMerma_Fecha_ProductoTerminadoId",
                table: "RegistrosMerma",
                columns: new[] { "Fecha", "ProductoTerminadoId" });

            migrationBuilder.CreateIndex(
                name: "IX_CuentasCorrientesProveedores_Fecha_ProveedorId",
                table: "CuentasCorrientesProveedores",
                columns: new[] { "Fecha", "ProveedorId" });

            migrationBuilder.CreateIndex(
                name: "IX_CuentasCorrientesBar_Fecha_BarId",
                table: "CuentasCorrientesBar",
                columns: new[] { "Fecha", "BarId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovimientosStock_Fecha_Tipo_InsumoId",
                table: "MovimientosStock");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosStock_Fecha_Tipo_ProductoTerminadoId",
                table: "MovimientosStock");

            migrationBuilder.DropIndex(
                name: "IX_Producciones_Fecha_Estado",
                table: "Producciones");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosHoras_Fecha_EmpleadoId",
                table: "RegistrosHoras");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosMerma_Fecha_ProductoTerminadoId",
                table: "RegistrosMerma");

            migrationBuilder.DropIndex(
                name: "IX_CuentasCorrientesProveedores_Fecha_ProveedorId",
                table: "CuentasCorrientesProveedores");

            migrationBuilder.DropIndex(
                name: "IX_CuentasCorrientesBar_Fecha_BarId",
                table: "CuentasCorrientesBar");
        }
    }
}
