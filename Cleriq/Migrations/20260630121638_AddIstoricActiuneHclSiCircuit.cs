using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddIstoricActiuneHclSiCircuit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AIntratInCircuit",
                table: "Hcluri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Backfill: actele deja intrate în circuit (publicate în MOL sau comunicate
            // prefectului) primesc latch-ul retroactiv, ca înghețarea variantei semnate să li se aplice.
            migrationBuilder.Sql(
                "UPDATE [Hcluri] SET [AIntratInCircuit] = 1 WHERE [DataPublicareMol] IS NOT NULL;");
            migrationBuilder.Sql(
                "UPDATE h SET h.[AIntratInCircuit] = 1 FROM [Hcluri] h " +
                "WHERE EXISTS (SELECT 1 FROM [ComunicariHclPrefect] c " +
                "WHERE c.[HclId] = h.[Id] AND c.[EsteSters] = 0);");

            migrationBuilder.CreateTable(
                name: "IstoricActiuniHcl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HclId = table.Column<int>(type: "int", nullable: false),
                    Tip = table.Column<int>(type: "int", nullable: false),
                    Motiv = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AdresaIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_IstoricActiuniHcl", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IstoricActiuniHcl_Hcluri_HclId",
                        column: x => x.HclId,
                        principalTable: "Hcluri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IstoricActiuniHcl_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IstoricActiuniHcl_HclId",
                table: "IstoricActiuniHcl",
                column: "HclId");

            migrationBuilder.CreateIndex(
                name: "IX_IstoricActiuniHcl_InstitutieId",
                table: "IstoricActiuniHcl",
                column: "InstitutieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IstoricActiuniHcl");

            migrationBuilder.DropColumn(
                name: "AIntratInCircuit",
                table: "Hcluri");
        }
    }
}
