using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_Inventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventarioSesiones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoInventario = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    ResponsableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventarioSesiones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventarioConteos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventarioSesionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsumoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductoTerminadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CantidadSistema = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadContada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventarioConteos", x => x.Id);
                    table.CheckConstraint("CK_InventarioConteo_UnSoloTarget", "([InsumoId] IS NOT NULL AND [ProductoTerminadoId] IS NULL) OR ([InsumoId] IS NULL AND [ProductoTerminadoId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_InventarioConteos_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventarioConteos_InventarioSesiones_InventarioSesionId",
                        column: x => x.InventarioSesionId,
                        principalTable: "InventarioSesiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventarioConteos_ProductosTerminados_ProductoTerminadoId",
                        column: x => x.ProductoTerminadoId,
                        principalTable: "ProductosTerminados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventarioConteos_InsumoId",
                table: "InventarioConteos",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_InventarioConteos_InventarioSesionId",
                table: "InventarioConteos",
                column: "InventarioSesionId");

            migrationBuilder.CreateIndex(
                name: "IX_InventarioConteos_ProductoTerminadoId",
                table: "InventarioConteos",
                column: "ProductoTerminadoId");

            migrationBuilder.CreateIndex(
                name: "IX_InventarioSesiones_Fecha_Estado",
                table: "InventarioSesiones",
                columns: new[] { "Fecha", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventarioConteos");

            migrationBuilder.DropTable(
                name: "InventarioSesiones");
        }
    }
}
