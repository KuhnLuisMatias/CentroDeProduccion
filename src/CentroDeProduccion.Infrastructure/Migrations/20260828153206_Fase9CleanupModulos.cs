using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase9CleanupModulos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PagoMetodo_Cheques_ChequeId",
                table: "PagoMetodo");

            migrationBuilder.DropTable(
                name: "Cheques");

            migrationBuilder.DropTable(
                name: "RegistrosHoras");

            migrationBuilder.DropTable(
                name: "RegistrosMerma");

            migrationBuilder.DropIndex(
                name: "IX_PagoMetodo_ChequeId",
                table: "PagoMetodo");

            migrationBuilder.DropColumn(
                name: "CostoTotalManoObra",
                table: "Producciones");

            migrationBuilder.DropColumn(
                name: "ChequeId",
                table: "PagoMetodo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoTotalManoObra",
                table: "Producciones",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ChequeId",
                table: "PagoMetodo",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cheques",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BancoEmisor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Beneficiario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstadoCheque = table.Column<int>(type: "int", nullable: false),
                    FechaCobro = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TipoCheque = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cheques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cheques_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosHoras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProduccionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HorasTrabajadas = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TarifaAplicada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosHoras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosHoras_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrosHoras_Producciones_ProduccionId",
                        column: x => x.ProduccionId,
                        principalTable: "Producciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosMerma",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsumoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductoTerminadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosMerma", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosMerma_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrosMerma_ProductosTerminados_ProductoTerminadoId",
                        column: x => x.ProductoTerminadoId,
                        principalTable: "ProductosTerminados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagoMetodo_ChequeId",
                table: "PagoMetodo",
                column: "ChequeId");

            migrationBuilder.CreateIndex(
                name: "IX_Cheques_Numero",
                table: "Cheques",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cheques_ProveedorId",
                table: "Cheques",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosHoras_EmpleadoId",
                table: "RegistrosHoras",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosHoras_Fecha_ProduccionId",
                table: "RegistrosHoras",
                columns: new[] { "Fecha", "ProduccionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosHoras_ProduccionId",
                table: "RegistrosHoras",
                column: "ProduccionId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosMerma_InsumoId",
                table: "RegistrosMerma",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosMerma_ProductoTerminadoId",
                table: "RegistrosMerma",
                column: "ProductoTerminadoId");

            migrationBuilder.AddForeignKey(
                name: "FK_PagoMetodo_Cheques_ChequeId",
                table: "PagoMetodo",
                column: "ChequeId",
                principalTable: "Cheques",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
