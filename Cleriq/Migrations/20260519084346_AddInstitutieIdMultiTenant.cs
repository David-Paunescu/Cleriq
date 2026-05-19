using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutieIdMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstitutieId",
                table: "Voturi",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InstitutieId",
                table: "PuncteOrdineZi",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InstitutieId",
                table: "ProceseVerbale",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InstitutieId",
                table: "Prezente",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InstitutieId",
                table: "Mandate",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InstitutieId",
                table: "ComisieMembri",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InstitutieId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstitutieId",
                table: "Voturi");

            migrationBuilder.DropColumn(
                name: "InstitutieId",
                table: "PuncteOrdineZi");

            migrationBuilder.DropColumn(
                name: "InstitutieId",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "InstitutieId",
                table: "Prezente");

            migrationBuilder.DropColumn(
                name: "InstitutieId",
                table: "Mandate");

            migrationBuilder.DropColumn(
                name: "InstitutieId",
                table: "ComisieMembri");

            migrationBuilder.DropColumn(
                name: "InstitutieId",
                table: "AspNetUsers");
        }
    }
}
