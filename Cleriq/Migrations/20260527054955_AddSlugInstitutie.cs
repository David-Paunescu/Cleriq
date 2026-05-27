using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddSlugInstitutie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pas 1: coloana nullable temporar (ca rândurile existente să poată trăi fără slug între pași)
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Institutii",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Pas 2: populează slug pentru rândurile existente
            // După cleanup, doar Slobozia (id=2) ar trebui să existe
            migrationBuilder.Sql(
                "UPDATE Institutii SET Slug = 'primaria-slobozia' WHERE Id = 2 AND Slug IS NULL;");

            // Defensiv: orice rând rămas fără slug primește un placeholder unic
            // (nu ar trebui să apară după cleanup, dar e centură de siguranță)
            migrationBuilder.Sql(
                "UPDATE Institutii SET Slug = CONCAT('institutie-', Id) WHERE Slug IS NULL;");

            // Pas 3: face coloana NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Institutii",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            // Pas 4: index unic GLOBAL (fără filter pe EsteSters — slug-uri arse pentru soft-deleted)
            migrationBuilder.CreateIndex(
                name: "IX_Institutii_Slug",
                table: "Institutii",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Institutii_Slug",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Institutii");
        }
    }
}