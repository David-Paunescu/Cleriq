using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddConvocare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Convocari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SedintaId = table.Column<int>(type: "int", nullable: false),
                    ConsilierId = table.Column<int>(type: "int", nullable: false),
                    EmailStatus = table.Column<int>(type: "int", nullable: true),
                    EmailTrimisLa = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailDetalii = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SmsStatus = table.Column<int>(type: "int", nullable: true),
                    SmsTrimisLa = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SmsDetalii = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InstitutieId = table.Column<int>(type: "int", nullable: false),
                    CreatLa = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatDe = table.Column<int>(type: "int", nullable: true),
                    ModificatLa = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificatDe = table.Column<int>(type: "int", nullable: true),
                    EsteSters = table.Column<bool>(type: "bit", nullable: false),
                    StersLa = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StersDe = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Convocari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Convocari_Consilieri_ConsilierId",
                        column: x => x.ConsilierId,
                        principalTable: "Consilieri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Convocari_Sedinte_SedintaId",
                        column: x => x.SedintaId,
                        principalTable: "Sedinte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Convocari_ConsilierId",
                table: "Convocari",
                column: "ConsilierId");

            migrationBuilder.CreateIndex(
                name: "IX_Convocari_SedintaId_ConsilierId",
                table: "Convocari",
                columns: new[] { "SedintaId", "ConsilierId" },
                unique: true,
                filter: "[EsteSters] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Convocari");
        }
    }
}
