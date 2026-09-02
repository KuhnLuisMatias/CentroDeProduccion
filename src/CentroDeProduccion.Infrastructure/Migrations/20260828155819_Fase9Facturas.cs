using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase9Facturas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagoProveedorItems");

            migrationBuilder.AddColumn<string>(
                name: "Referencia",
                table: "PagoMetodo",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PagoInsumo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PagoProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsumoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagoInsumo", x => new { x.PagoProveedorId, x.Id });
                    table.ForeignKey(
                        name: "FK_PagoInsumo_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagoInsumo_PagosProveedor_PagoProveedorId",
                        column: x => x.PagoProveedorId,
                        principalTable: "PagosProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagoInsumo_InsumoId",
                table: "PagoInsumo",
                column: "InsumoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagoInsumo");

            migrationBuilder.DropColumn(
                name: "Referencia",
                table: "PagoMetodo");

            migrationBuilder.CreateTable(
                name: "PagoProveedorItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenCompraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PagoProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MontoAplicado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagoProveedorItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagoProveedorItems_OrdenesCompra_OrdenCompraId",
                        column: x => x.OrdenCompraId,
                        principalTable: "OrdenesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagoProveedorItems_PagosProveedor_PagoProveedorId",
                        column: x => x.PagoProveedorId,
                        principalTable: "PagosProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagoProveedorItems_OrdenCompraId",
                table: "PagoProveedorItems",
                column: "OrdenCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_PagoProveedorItems_PagoProveedorId",
                table: "PagoProveedorItems",
                column: "PagoProveedorId");
        }
    }
}
