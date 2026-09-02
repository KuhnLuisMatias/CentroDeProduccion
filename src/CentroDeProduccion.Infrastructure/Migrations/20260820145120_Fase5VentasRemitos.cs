using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase5VentasRemitos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Encargado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HorarioRecepcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MargenReventaPorcentaje = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PagosBar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    BarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosBar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosBar_Bares_BarId",
                        column: x => x.BarId,
                        principalTable: "Bares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Remitos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroRemito = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntregadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecibidoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Remitos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Remitos_Bares_BarId",
                        column: x => x.BarId,
                        principalTable: "Bares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagoBarMetodo",
                columns: table => new
                {
                    PagoBarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagoBarMetodo", x => new { x.PagoBarId, x.Id });
                    table.ForeignKey(
                        name: "FK_PagoBarMetodo_PagosBar_PagoBarId",
                        column: x => x.PagoBarId,
                        principalTable: "PagosBar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Devoluciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    RemitoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecibidoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devoluciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devoluciones_Remitos_RemitoId",
                        column: x => x.RemitoId,
                        principalTable: "Remitos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagosBarItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PagoBarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemitoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MontoAplicado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosBarItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosBarItems_PagosBar_PagoBarId",
                        column: x => x.PagoBarId,
                        principalTable: "PagosBar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PagosBarItems_Remitos_RemitoId",
                        column: x => x.RemitoId,
                        principalTable: "Remitos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RemitoLineas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemitoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoLinea = table.Column<int>(type: "int", nullable: false),
                    ProductoTerminadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InsumoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Lote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemitoLineas", x => x.Id);
                    table.CheckConstraint("CK_RemitoLinea_UnSoloTarget", "([ProductoTerminadoId] IS NOT NULL AND [InsumoId] IS NULL) OR ([ProductoTerminadoId] IS NULL AND [InsumoId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_RemitoLineas_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RemitoLineas_ProductosTerminados_ProductoTerminadoId",
                        column: x => x.ProductoTerminadoId,
                        principalTable: "ProductosTerminados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RemitoLineas_Remitos_RemitoId",
                        column: x => x.RemitoId,
                        principalTable: "Remitos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CuentasCorrientesBar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoMovimiento = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemitoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DevolucionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PagoBarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasCorrientesBar", x => x.Id);
                    table.CheckConstraint("CK_CuentaCorrienteBar_MontoSigno", "([TipoMovimiento] IN (1,4,6) AND [Monto] > 0) OR ([TipoMovimiento] IN (2,3,5) AND [Monto] < 0)");
                    table.ForeignKey(
                        name: "FK_CuentasCorrientesBar_Bares_BarId",
                        column: x => x.BarId,
                        principalTable: "Bares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuentasCorrientesBar_Devoluciones_DevolucionId",
                        column: x => x.DevolucionId,
                        principalTable: "Devoluciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuentasCorrientesBar_PagosBar_PagoBarId",
                        column: x => x.PagoBarId,
                        principalTable: "PagosBar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuentasCorrientesBar_Remitos_RemitoId",
                        column: x => x.RemitoId,
                        principalTable: "Remitos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DevolucionLineas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DevolucionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoTerminadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Lote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionLineas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevolucionLineas_Devoluciones_DevolucionId",
                        column: x => x.DevolucionId,
                        principalTable: "Devoluciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DevolucionLineas_ProductosTerminados_ProductoTerminadoId",
                        column: x => x.ProductoTerminadoId,
                        principalTable: "ProductosTerminados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bares_Nombre",
                table: "Bares",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuentasCorrientesBar_BarId",
                table: "CuentasCorrientesBar",
                column: "BarId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasCorrientesBar_DevolucionId",
                table: "CuentasCorrientesBar",
                column: "DevolucionId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasCorrientesBar_PagoBarId",
                table: "CuentasCorrientesBar",
                column: "PagoBarId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasCorrientesBar_RemitoId",
                table: "CuentasCorrientesBar",
                column: "RemitoId");

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_Numero",
                table: "Devoluciones",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_RemitoId",
                table: "Devoluciones",
                column: "RemitoId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionLineas_DevolucionId",
                table: "DevolucionLineas",
                column: "DevolucionId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionLineas_ProductoTerminadoId",
                table: "DevolucionLineas",
                column: "ProductoTerminadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosBar_BarId",
                table: "PagosBar",
                column: "BarId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosBar_Numero",
                table: "PagosBar",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagosBarItems_PagoBarId",
                table: "PagosBarItems",
                column: "PagoBarId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosBarItems_RemitoId",
                table: "PagosBarItems",
                column: "RemitoId");

            migrationBuilder.CreateIndex(
                name: "IX_RemitoLineas_InsumoId",
                table: "RemitoLineas",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_RemitoLineas_ProductoTerminadoId",
                table: "RemitoLineas",
                column: "ProductoTerminadoId");

            migrationBuilder.CreateIndex(
                name: "IX_RemitoLineas_RemitoId",
                table: "RemitoLineas",
                column: "RemitoId");

            migrationBuilder.CreateIndex(
                name: "IX_Remitos_BarId",
                table: "Remitos",
                column: "BarId");

            migrationBuilder.CreateIndex(
                name: "IX_Remitos_NumeroRemito",
                table: "Remitos",
                column: "NumeroRemito",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuentasCorrientesBar");

            migrationBuilder.DropTable(
                name: "DevolucionLineas");

            migrationBuilder.DropTable(
                name: "PagoBarMetodo");

            migrationBuilder.DropTable(
                name: "PagosBarItems");

            migrationBuilder.DropTable(
                name: "RemitoLineas");

            migrationBuilder.DropTable(
                name: "Devoluciones");

            migrationBuilder.DropTable(
                name: "PagosBar");

            migrationBuilder.DropTable(
                name: "Remitos");

            migrationBuilder.DropTable(
                name: "Bares");
        }
    }
}
