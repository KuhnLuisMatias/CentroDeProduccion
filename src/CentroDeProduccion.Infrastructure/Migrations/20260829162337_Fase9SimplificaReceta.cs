using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase9SimplificaReceta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recetas_UnidadesMedida_UnidadRendimientoId",
                table: "Recetas");

            migrationBuilder.DropIndex(
                name: "IX_Recetas_UnidadRendimientoId",
                table: "Recetas");

            migrationBuilder.DropColumn(
                name: "CostoMetodo",
                table: "RecetaVersiones");

            migrationBuilder.DropColumn(
                name: "MermaEstimada",
                table: "RecetaVersiones");

            migrationBuilder.DropColumn(
                name: "Rendimiento",
                table: "RecetaVersiones");

            migrationBuilder.DropColumn(
                name: "CostoMetodo",
                table: "Recetas");

            migrationBuilder.DropColumn(
                name: "MermaEstimada",
                table: "Recetas");

            migrationBuilder.DropColumn(
                name: "Rendimiento",
                table: "Recetas");

            migrationBuilder.DropColumn(
                name: "TiempoProduccionEstimado",
                table: "Recetas");

            migrationBuilder.DropColumn(
                name: "UnidadRendimientoId",
                table: "Recetas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CostoMetodo",
                table: "RecetaVersiones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MermaEstimada",
                table: "RecetaVersiones",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Rendimiento",
                table: "RecetaVersiones",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CostoMetodo",
                table: "Recetas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MermaEstimada",
                table: "Recetas",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Rendimiento",
                table: "Recetas",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TiempoProduccionEstimado",
                table: "Recetas",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnidadRendimientoId",
                table: "Recetas",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Recetas_UnidadRendimientoId",
                table: "Recetas",
                column: "UnidadRendimientoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recetas_UnidadesMedida_UnidadRendimientoId",
                table: "Recetas",
                column: "UnidadRendimientoId",
                principalTable: "UnidadesMedida",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
