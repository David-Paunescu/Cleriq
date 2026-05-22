using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddFusOrarInstitutie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FusOrar",
                table: "Institutii",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Europe/Bucharest");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FusOrar",
                table: "Institutii");
        }
    }
}
