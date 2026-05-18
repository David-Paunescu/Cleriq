using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddProcesVerbal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProceseVerbale",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Continut = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataGenerare = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataFinalizare = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SedintaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProceseVerbale", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProceseVerbale_Sedinte_SedintaId",
                        column: x => x.SedintaId,
                        principalTable: "Sedinte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProceseVerbale_SedintaId",
                table: "ProceseVerbale",
                column: "SedintaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProceseVerbale");
        }
    }
}
