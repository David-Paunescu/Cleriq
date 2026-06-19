using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddAprobarePv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AprobatDe",
                table: "ProceseVerbale",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AprobatInSedintaId",
                table: "ProceseVerbale",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataAprobare",
                table: "ProceseVerbale",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProceseVerbale_AprobatInSedintaId",
                table: "ProceseVerbale",
                column: "AprobatInSedintaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProceseVerbale_Sedinte_AprobatInSedintaId",
                table: "ProceseVerbale",
                column: "AprobatInSedintaId",
                principalTable: "Sedinte",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProceseVerbale_Sedinte_AprobatInSedintaId",
                table: "ProceseVerbale");

            migrationBuilder.DropIndex(
                name: "IX_ProceseVerbale_AprobatInSedintaId",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "AprobatDe",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "AprobatInSedintaId",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "DataAprobare",
                table: "ProceseVerbale");
        }
    }
}
