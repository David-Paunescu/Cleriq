using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddPunctOrdineZi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PuncteOrdineZi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ordine = table.Column<int>(type: "int", nullable: false),
                    Titlu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Descriere = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tip = table.Column<int>(type: "int", nullable: false),
                    NecesitaVot = table.Column<bool>(type: "bit", nullable: false),
                    TipMajoritate = table.Column<int>(type: "int", nullable: true),
                    Rezultat = table.Column<int>(type: "int", nullable: true),
                    SedintaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuncteOrdineZi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuncteOrdineZi_Sedinte_SedintaId",
                        column: x => x.SedintaId,
                        principalTable: "Sedinte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PuncteOrdineZi_SedintaId",
                table: "PuncteOrdineZi",
                column: "SedintaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PuncteOrdineZi");
        }
    }
}
