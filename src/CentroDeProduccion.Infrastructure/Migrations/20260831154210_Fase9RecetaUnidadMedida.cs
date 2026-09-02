using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase9RecetaUnidadMedida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UnidadMedidaId",
                table: "Recetas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recetas_UnidadMedidaId",
                table: "Recetas",
                column: "UnidadMedidaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recetas_UnidadesMedida_UnidadMedidaId",
                table: "Recetas",
                column: "UnidadMedidaId",
                principalTable: "UnidadesMedida",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recetas_UnidadesMedida_UnidadMedidaId",
                table: "Recetas");

            migrationBuilder.DropIndex(
                name: "IX_Recetas_UnidadMedidaId",
                table: "Recetas");

            migrationBuilder.DropColumn(
                name: "UnidadMedidaId",
                table: "Recetas");
        }
    }
}
