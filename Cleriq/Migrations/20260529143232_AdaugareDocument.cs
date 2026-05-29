using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AdaugareDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Denumire = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Descriere = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TipDocument = table.Column<int>(type: "int", nullable: false),
                    NumeFisierOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TipMime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Marime = table.Column<long>(type: "bigint", nullable: false),
                    HashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CaleStocare = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EstePublic = table.Column<bool>(type: "bit", nullable: false),
                    Ordine = table.Column<int>(type: "int", nullable: false),
                    SedintaId = table.Column<int>(type: "int", nullable: true),
                    PunctId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Documente", x => x.Id);
                    table.CheckConstraint("CK_Document_ExactUnContext", "(CASE WHEN [SedintaId] IS NULL THEN 0 ELSE 1 END +  CASE WHEN [PunctId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_Documente_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documente_PuncteOrdineZi_PunctId",
                        column: x => x.PunctId,
                        principalTable: "PuncteOrdineZi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documente_Sedinte_SedintaId",
                        column: x => x.SedintaId,
                        principalTable: "Sedinte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documente_InstitutieId",
                table: "Documente",
                column: "InstitutieId");

            migrationBuilder.CreateIndex(
                name: "IX_Documente_PunctId",
                table: "Documente",
                column: "PunctId");

            migrationBuilder.CreateIndex(
                name: "IX_Documente_SedintaId",
                table: "Documente",
                column: "SedintaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documente");
        }
    }
}
