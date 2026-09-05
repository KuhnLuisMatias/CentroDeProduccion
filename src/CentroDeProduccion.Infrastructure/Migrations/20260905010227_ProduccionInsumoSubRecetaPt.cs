using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroDeProduccion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProduccionInsumoSubRecetaPt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "InsumoId",
                table: "ProduccionInsumos",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "RecetaOrigenId",
                table: "ProduccionInsumos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProduccionInsumos_RecetaOrigenId",
                table: "ProduccionInsumos",
                column: "RecetaOrigenId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProduccionInsumos_UnSoloOrigen",
                table: "ProduccionInsumos",
                sql: "([InsumoId] IS NOT NULL AND [RecetaOrigenId] IS NULL) OR ([InsumoId] IS NULL AND [RecetaOrigenId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_ProduccionInsumos_Recetas_RecetaOrigenId",
                table: "ProduccionInsumos",
                column: "RecetaOrigenId",
                principalTable: "Recetas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProduccionInsumos_Recetas_RecetaOrigenId",
                table: "ProduccionInsumos");

            migrationBuilder.DropIndex(
                name: "IX_ProduccionInsumos_RecetaOrigenId",
                table: "ProduccionInsumos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProduccionInsumos_UnSoloOrigen",
                table: "ProduccionInsumos");

            migrationBuilder.DropColumn(
                name: "RecetaOrigenId",
                table: "ProduccionInsumos");

            migrationBuilder.AlterColumn<Guid>(
                name: "InsumoId",
                table: "ProduccionInsumos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
