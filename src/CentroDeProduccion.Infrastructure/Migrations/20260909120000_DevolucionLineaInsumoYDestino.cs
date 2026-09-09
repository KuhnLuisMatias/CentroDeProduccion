using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DevolucionLineaInsumoYDestino : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ProductoTerminadoId",
                table: "DevolucionLineas",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "InsumoId",
                table: "DevolucionLineas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Destino",
                table: "DevolucionLineas",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionLineas_InsumoId",
                table: "DevolucionLineas",
                column: "InsumoId");

            migrationBuilder.AddForeignKey(
                name: "FK_DevolucionLineas_Insumos_InsumoId",
                table: "DevolucionLineas",
                column: "InsumoId",
                principalTable: "Insumos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DevolucionLineas_Insumos_InsumoId",
                table: "DevolucionLineas");

            migrationBuilder.DropIndex(
                name: "IX_DevolucionLineas_InsumoId",
                table: "DevolucionLineas");

            migrationBuilder.DropColumn(
                name: "Destino",
                table: "DevolucionLineas");

            migrationBuilder.DropColumn(
                name: "InsumoId",
                table: "DevolucionLineas");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductoTerminadoId",
                table: "DevolucionLineas",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
