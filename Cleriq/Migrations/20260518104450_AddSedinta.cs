using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddSedinta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sedinte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titlu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Numar = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Tip = table.Column<int>(type: "int", nullable: false),
                    DataOra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Loc = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ModDesfasurare = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConvocareTrimisaLa = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InstitutieId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sedinte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sedinte_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sedinte_InstitutieId",
                table: "Sedinte",
                column: "InstitutieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sedinte");
        }
    }
}
