using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class RenameNumarIncercariToNumarEsecuri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NumarIncercari",
                table: "Transcrieri",
                newName: "NumarEsecuri");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NumarEsecuri",
                table: "Transcrieri",
                newName: "NumarIncercari");
        }
    }
}
