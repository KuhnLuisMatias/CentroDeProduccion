using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase2RecetasProduccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "InsumoId",
                table: "MovimientosStock",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "ProduccionId",
                table: "MovimientosStock",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductoTerminadoId",
                table: "MovimientosStock",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CantidadAcumuladaCompras",
                table: "Insumos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ProductosTerminados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoSku = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadMedidaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockActual = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StockMinimo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FechaProduccion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Lote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductosTerminados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductosTerminados_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductosTerminados_UnidadesMedida_UnidadMedidaId",
                        column: x => x.UnidadMedidaId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recetas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoSku = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rendimiento = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnidadRendimientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MermaEstimada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TiempoProduccionEstimado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    CostoMetodo = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recetas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recetas_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recetas_UnidadesMedida_UnidadRendimientoId",
                        column: x => x.UnidadRendimientoId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PresentacionesVenta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnidadMedidaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresentacionesVenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PresentacionesVenta_Recetas_RecetaId",
                        column: x => x.RecetaId,
                        principalTable: "Recetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PresentacionesVenta_UnidadesMedida_UnidadMedidaId",
                        column: x => x.UnidadMedidaId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Producciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResponsableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CantidadProducida = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CostoTotalInsumos = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Producciones_Recetas_RecetaId",
                        column: x => x.RecetaId,
                        principalTable: "Recetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Producciones_Usuarios_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecetaInsumos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsumoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecetaOrigenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CantidadNecesaria = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnidadMedidaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecetaInsumos", x => x.Id);
                    table.CheckConstraint("CK_RecetaInsumos_UnSoloOrigen", "([InsumoId] IS NOT NULL AND [RecetaOrigenId] IS NULL) OR ([InsumoId] IS NULL AND [RecetaOrigenId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_RecetaInsumos_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecetaInsumos_Recetas_RecetaId",
                        column: x => x.RecetaId,
                        principalTable: "Recetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecetaInsumos_Recetas_RecetaOrigenId",
                        column: x => x.RecetaOrigenId,
                        principalTable: "Recetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecetaInsumos_UnidadesMedida_UnidadMedidaId",
                        column: x => x.UnidadMedidaId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecetaVersiones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoSku = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rendimiento = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MermaEstimada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoMetodo = table.Column<int>(type: "int", nullable: false),
                    DetallesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecetaVersiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecetaVersiones_Recetas_RecetaId",
                        column: x => x.RecetaId,
                        principalTable: "Recetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProduccionSalidas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProduccionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoTerminadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TipoSalida = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProduccionSalidas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProduccionSalidas_Producciones_ProduccionId",
                        column: x => x.ProduccionId,
                        principalTable: "Producciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProduccionSalidas_ProductosTerminados_ProductoTerminadoId",
                        column: x => x.ProductoTerminadoId,
                        principalTable: "ProductosTerminados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_ProduccionId",
                table: "MovimientosStock",
                column: "ProduccionId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_ProductoTerminadoId",
                table: "MovimientosStock",
                column: "ProductoTerminadoId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MovimientosStock_UnSoloTarget",
                table: "MovimientosStock",
                sql: "([InsumoId] IS NOT NULL AND [ProductoTerminadoId] IS NULL) OR ([InsumoId] IS NULL AND [ProductoTerminadoId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_PresentacionesVenta_RecetaId",
                table: "PresentacionesVenta",
                column: "RecetaId");

            migrationBuilder.CreateIndex(
                name: "IX_PresentacionesVenta_UnidadMedidaId",
                table: "PresentacionesVenta",
                column: "UnidadMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Producciones_RecetaId",
                table: "Producciones",
                column: "RecetaId");

            migrationBuilder.CreateIndex(
                name: "IX_Producciones_ResponsableId",
                table: "Producciones",
                column: "ResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_ProduccionSalidas_ProduccionId",
                table: "ProduccionSalidas",
                column: "ProduccionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProduccionSalidas_ProductoTerminadoId",
                table: "ProduccionSalidas",
                column: "ProductoTerminadoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductosTerminados_CategoriaId",
                table: "ProductosTerminados",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductosTerminados_CodigoSku",
                table: "ProductosTerminados",
                column: "CodigoSku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductosTerminados_UnidadMedidaId",
                table: "ProductosTerminados",
                column: "UnidadMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaInsumos_InsumoId",
                table: "RecetaInsumos",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaInsumos_RecetaId",
                table: "RecetaInsumos",
                column: "RecetaId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaInsumos_RecetaOrigenId",
                table: "RecetaInsumos",
                column: "RecetaOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaInsumos_UnidadMedidaId",
                table: "RecetaInsumos",
                column: "UnidadMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Recetas_CategoriaId",
                table: "Recetas",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Recetas_CodigoSku",
                table: "Recetas",
                column: "CodigoSku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recetas_UnidadRendimientoId",
                table: "Recetas",
                column: "UnidadRendimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaVersiones_RecetaId_Version",
                table: "RecetaVersiones",
                columns: new[] { "RecetaId", "Version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Producciones_ProduccionId",
                table: "MovimientosStock",
                column: "ProduccionId",
                principalTable: "Producciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_ProductosTerminados_ProductoTerminadoId",
                table: "MovimientosStock",
                column: "ProductoTerminadoId",
                principalTable: "ProductosTerminados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Producciones_ProduccionId",
                table: "MovimientosStock");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_ProductosTerminados_ProductoTerminadoId",
                table: "MovimientosStock");

            migrationBuilder.DropTable(
                name: "PresentacionesVenta");

            migrationBuilder.DropTable(
                name: "ProduccionSalidas");

            migrationBuilder.DropTable(
                name: "RecetaInsumos");

            migrationBuilder.DropTable(
                name: "RecetaVersiones");

            migrationBuilder.DropTable(
                name: "Producciones");

            migrationBuilder.DropTable(
                name: "ProductosTerminados");

            migrationBuilder.DropTable(
                name: "Recetas");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosStock_ProduccionId",
                table: "MovimientosStock");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosStock_ProductoTerminadoId",
                table: "MovimientosStock");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MovimientosStock_UnSoloTarget",
                table: "MovimientosStock");

            migrationBuilder.DropColumn(
                name: "ProduccionId",
                table: "MovimientosStock");

            migrationBuilder.DropColumn(
                name: "ProductoTerminadoId",
                table: "MovimientosStock");

            migrationBuilder.DropColumn(
                name: "CantidadAcumuladaCompras",
                table: "Insumos");

            migrationBuilder.AlterColumn<Guid>(
                name: "InsumoId",
                table: "MovimientosStock",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
