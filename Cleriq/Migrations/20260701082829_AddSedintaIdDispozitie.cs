using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddSedintaIdDispozitie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SedintaId",
                table: "Dispozitii",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dispozitii_SedintaId",
                table: "Dispozitii",
                column: "SedintaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dispozitii_Sedinte_SedintaId",
                table: "Dispozitii",
                column: "SedintaId",
                principalTable: "Sedinte",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dispozitii_Sedinte_SedintaId",
                table: "Dispozitii");

            migrationBuilder.DropIndex(
                name: "IX_Dispozitii_SedintaId",
                table: "Dispozitii");

            migrationBuilder.DropColumn(
                name: "SedintaId",
                table: "Dispozitii");
        }
    }
}
