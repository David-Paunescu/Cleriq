using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddComisii : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comisii",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Denumire = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descriere = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InstitutieId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comisii", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comisii_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComisieMembri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComisieId = table.Column<int>(type: "int", nullable: false),
                    ConsilierId = table.Column<int>(type: "int", nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComisieMembri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComisieMembri_Comisii_ComisieId",
                        column: x => x.ComisieId,
                        principalTable: "Comisii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComisieMembri_Consilieri_ConsilierId",
                        column: x => x.ConsilierId,
                        principalTable: "Consilieri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComisieMembri_ComisieId",
                table: "ComisieMembri",
                column: "ComisieId");

            migrationBuilder.CreateIndex(
                name: "IX_ComisieMembri_ConsilierId",
                table: "ComisieMembri",
                column: "ConsilierId");

            migrationBuilder.CreateIndex(
                name: "IX_Comisii_InstitutieId",
                table: "Comisii",
                column: "InstitutieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComisieMembri");

            migrationBuilder.DropTable(
                name: "Comisii");
        }
    }
}
