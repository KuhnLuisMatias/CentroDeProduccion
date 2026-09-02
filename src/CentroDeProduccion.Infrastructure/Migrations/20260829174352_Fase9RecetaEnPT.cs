using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase9RecetaEnPT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanalPedido",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "CondicionesPago",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "MontoMinimoCompra",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "TiempoEntrega",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "CostoUnitario",
                table: "ProductosTerminados");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "OrdenesCompra");

            migrationBuilder.AddColumn<Guid>(
                name: "RecetaId",
                table: "ProductosTerminados",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductosTerminados_RecetaId",
                table: "ProductosTerminados",
                column: "RecetaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductosTerminados_Recetas_RecetaId",
                table: "ProductosTerminados",
                column: "RecetaId",
                principalTable: "Recetas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductosTerminados_Recetas_RecetaId",
                table: "ProductosTerminados");

            migrationBuilder.DropIndex(
                name: "IX_ProductosTerminados_RecetaId",
                table: "ProductosTerminados");

            migrationBuilder.DropColumn(
                name: "RecetaId",
                table: "ProductosTerminados");

            migrationBuilder.AddColumn<string>(
                name: "CanalPedido",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CondicionesPago",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoMinimoCompra",
                table: "Proveedores",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TiempoEntrega",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoUnitario",
                table: "ProductosTerminados",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MetodoPago",
                table: "OrdenesCompra",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
