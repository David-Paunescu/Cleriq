using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddPrezentaVot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prezente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OraSosire = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SedintaId = table.Column<int>(type: "int", nullable: false),
                    ConsilierId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prezente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prezente_Consilieri_ConsilierId",
                        column: x => x.ConsilierId,
                        principalTable: "Consilieri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prezente_Sedinte_SedintaId",
                        column: x => x.SedintaId,
                        principalTable: "Sedinte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Voturi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Optiune = table.Column<int>(type: "int", nullable: false),
                    DataOra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PunctId = table.Column<int>(type: "int", nullable: false),
                    ConsilierId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voturi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Voturi_Consilieri_ConsilierId",
                        column: x => x.ConsilierId,
                        principalTable: "Consilieri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Voturi_PuncteOrdineZi_PunctId",
                        column: x => x.PunctId,
                        principalTable: "PuncteOrdineZi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prezente_ConsilierId",
                table: "Prezente",
                column: "ConsilierId");

            migrationBuilder.CreateIndex(
                name: "IX_Prezente_SedintaId_ConsilierId",
                table: "Prezente",
                columns: new[] { "SedintaId", "ConsilierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Voturi_ConsilierId",
                table: "Voturi",
                column: "ConsilierId");

            migrationBuilder.CreateIndex(
                name: "IX_Voturi_PunctId_ConsilierId",
                table: "Voturi",
                columns: new[] { "PunctId", "ConsilierId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Prezente");

            migrationBuilder.DropTable(
                name: "Voturi");
        }
    }
}
