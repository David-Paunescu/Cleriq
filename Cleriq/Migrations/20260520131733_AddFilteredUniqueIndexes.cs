using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddFilteredUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Voturi_PunctId_ConsilierId",
                table: "Voturi");

            migrationBuilder.DropIndex(
                name: "IX_Prezente_SedintaId_ConsilierId",
                table: "Prezente");

            migrationBuilder.DropIndex(
                name: "IX_ComisieMembri_ComisieId_ConsilierId",
                table: "ComisieMembri");

            migrationBuilder.CreateIndex(
                name: "IX_Voturi_PunctId_ConsilierId",
                table: "Voturi",
                columns: new[] { "PunctId", "ConsilierId" },
                unique: true,
                filter: "[EsteSters] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Prezente_SedintaId_ConsilierId",
                table: "Prezente",
                columns: new[] { "SedintaId", "ConsilierId" },
                unique: true,
                filter: "[EsteSters] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ComisieMembri_ComisieId_ConsilierId",
                table: "ComisieMembri",
                columns: new[] { "ComisieId", "ConsilierId" },
                unique: true,
                filter: "[EsteSters] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Voturi_PunctId_ConsilierId",
                table: "Voturi");

            migrationBuilder.DropIndex(
                name: "IX_Prezente_SedintaId_ConsilierId",
                table: "Prezente");

            migrationBuilder.DropIndex(
                name: "IX_ComisieMembri_ComisieId_ConsilierId",
                table: "ComisieMembri");

            migrationBuilder.CreateIndex(
                name: "IX_Voturi_PunctId_ConsilierId",
                table: "Voturi",
                columns: new[] { "PunctId", "ConsilierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prezente_SedintaId_ConsilierId",
                table: "Prezente",
                columns: new[] { "SedintaId", "ConsilierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComisieMembri_ComisieId_ConsilierId",
                table: "ComisieMembri",
                columns: new[] { "ComisieId", "ConsilierId" },
                unique: true);
        }
    }
}
