using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueComisieMembru : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComisieMembri_ComisieId",
                table: "ComisieMembri");

            migrationBuilder.CreateIndex(
                name: "IX_ComisieMembri_ComisieId_ConsilierId",
                table: "ComisieMembri",
                columns: new[] { "ComisieId", "ConsilierId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComisieMembri_ComisieId_ConsilierId",
                table: "ComisieMembri");

            migrationBuilder.CreateIndex(
                name: "IX_ComisieMembri_ComisieId",
                table: "ComisieMembri",
                column: "ComisieId");
        }
    }
}
