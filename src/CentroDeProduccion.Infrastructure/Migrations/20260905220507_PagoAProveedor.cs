using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PagoAProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PagosAProveedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FacturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosAProveedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosAProveedores_PagosProveedor_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "PagosProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagosAProveedores_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagosAProveedoresMetodos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PagoAProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosAProveedoresMetodos", x => new { x.PagoAProveedorId, x.Id });
                    table.ForeignKey(
                        name: "FK_PagosAProveedoresMetodos_PagosAProveedores_PagoAProveedorId",
                        column: x => x.PagoAProveedorId,
                        principalTable: "PagosAProveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagosAProveedores_FacturaId",
                table: "PagosAProveedores",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosAProveedores_ProveedorId",
                table: "PagosAProveedores",
                column: "ProveedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagosAProveedoresMetodos");

            migrationBuilder.DropTable(
                name: "PagosAProveedores");
        }
    }
}
