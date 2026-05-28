using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddConsilierIdUtilizator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsilierId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ConsilierId",
                table: "AspNetUsers",
                column: "ConsilierId",
                unique: true,
                filter: "[ConsilierId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Consilieri_ConsilierId",
                table: "AspNetUsers",
                column: "ConsilierId",
                principalTable: "Consilieri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Consilieri_ConsilierId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ConsilierId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ConsilierId",
                table: "AspNetUsers");
        }
    }
}
