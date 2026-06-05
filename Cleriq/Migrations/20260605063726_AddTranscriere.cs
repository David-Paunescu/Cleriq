using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscriere : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transcrieri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SedintaId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ContinutBrut = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContinutEditat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataPrimireBrut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataUltimeiEditari = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CaleStocareAudio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DimensiuneAudio = table.Column<long>(type: "bigint", nullable: false),
                    DurataAudioSecunde = table.Column<int>(type: "int", nullable: true),
                    ModelFolosit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PromptFolosit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumarIncercari = table.Column<int>(type: "int", nullable: false),
                    UrmatoareaIncercareDupa = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimaEroare = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_Transcrieri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transcrieri_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transcrieri_Sedinte_SedintaId",
                        column: x => x.SedintaId,
                        principalTable: "Sedinte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transcrieri_InstitutieId",
                table: "Transcrieri",
                column: "InstitutieId");

            migrationBuilder.CreateIndex(
                name: "IX_Transcrieri_SedintaId",
                table: "Transcrieri",
                column: "SedintaId",
                unique: true,
                filter: "[EsteSters] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transcrieri");
        }
    }
}
