using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Institutii",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Denumire = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Judet = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodSiruta = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Tip = table.Column<int>(type: "int", nullable: false),
                    StatusAbonament = table.Column<int>(type: "int", nullable: false),
                    DataExpirare = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Institutii", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Institutii");
        }
    }
}
