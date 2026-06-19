using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddTrasabilitateFunctii : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComisieMembri_ComisieId_ConsilierId",
                table: "ComisieMembri");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataInceput",
                table: "ComisieMembri",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<bool>(
                name: "DataInceputEstimata",
                table: "ComisieMembri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataSfarsit",
                table: "ComisieMembri",
                type: "date",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE [ComisieMembri]
                SET [DataInceput] = CAST([CreatLa] AS date),
                    [DataInceputEstimata] = 1");

            migrationBuilder.CreateTable(
                name: "Persoane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeComplet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
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
                    table.PrimaryKey("PK_Persoane", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Persoane_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MandateFunctie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipFunctie = table.Column<int>(type: "int", nullable: false),
                    PersoanaId = table.Column<int>(type: "int", nullable: true),
                    ConsilierId = table.Column<int>(type: "int", nullable: true),
                    DataInceput = table.Column<DateOnly>(type: "date", nullable: false),
                    DataSfarsit = table.Column<DateOnly>(type: "date", nullable: true),
                    NrActNumire = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_MandateFunctie", x => x.Id);
                    table.CheckConstraint("CK_MandatFunctie_ExactUnSubject", "(CASE WHEN [PersoanaId] IS NULL THEN 0 ELSE 1 END +  CASE WHEN [ConsilierId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.CheckConstraint("CK_MandatFunctie_FkCorectaPerTip", "([TipFunctie] IN (1, 3) AND [PersoanaId] IS NOT NULL AND [ConsilierId] IS NULL) OR ([TipFunctie] = 2 AND [ConsilierId] IS NOT NULL AND [PersoanaId] IS NULL)");
                    table.CheckConstraint("CK_MandatFunctie_PerioadaValida", "[DataSfarsit] IS NULL OR [DataSfarsit] >= [DataInceput]");
                    table.ForeignKey(
                        name: "FK_MandateFunctie_Consilieri_ConsilierId",
                        column: x => x.ConsilierId,
                        principalTable: "Consilieri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MandateFunctie_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MandateFunctie_Persoane_PersoanaId",
                        column: x => x.PersoanaId,
                        principalTable: "Persoane",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComisieMembri_ComisieId_ConsilierId",
                table: "ComisieMembri",
                columns: new[] { "ComisieId", "ConsilierId" },
                unique: true,
                filter: "[EsteSters] = 0 AND [DataSfarsit] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MandateFunctie_ConsilierId",
                table: "MandateFunctie",
                column: "ConsilierId");

            migrationBuilder.CreateIndex(
                name: "IX_MandateFunctie_PersoanaId",
                table: "MandateFunctie",
                column: "PersoanaId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatFunctie_PrimarActiv",
                table: "MandateFunctie",
                column: "InstitutieId",
                unique: true,
                filter: "[EsteSters] = 0 AND [DataSfarsit] IS NULL AND [TipFunctie] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_MandatFunctie_SecretarUatActiv",
                table: "MandateFunctie",
                column: "InstitutieId",
                unique: true,
                filter: "[EsteSters] = 0 AND [DataSfarsit] IS NULL AND [TipFunctie] = 3");

            migrationBuilder.CreateIndex(
                name: "IX_MandatFunctie_ViceprimarActivPerConsilier",
                table: "MandateFunctie",
                columns: new[] { "InstitutieId", "ConsilierId" },
                unique: true,
                filter: "[EsteSters] = 0 AND [DataSfarsit] IS NULL AND [TipFunctie] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_Persoane_InstitutieId_NumeComplet",
                table: "Persoane",
                columns: new[] { "InstitutieId", "NumeComplet" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MandateFunctie");

            migrationBuilder.DropTable(
                name: "Persoane");

            migrationBuilder.DropIndex(
                name: "IX_ComisieMembri_ComisieId_ConsilierId",
                table: "ComisieMembri");

            migrationBuilder.DropColumn(
                name: "DataInceput",
                table: "ComisieMembri");

            migrationBuilder.DropColumn(
                name: "DataInceputEstimata",
                table: "ComisieMembri");

            migrationBuilder.DropColumn(
                name: "DataSfarsit",
                table: "ComisieMembri");

            migrationBuilder.CreateIndex(
                name: "IX_ComisieMembri_ComisieId_ConsilierId",
                table: "ComisieMembri",
                columns: new[] { "ComisieId", "ConsilierId" },
                unique: true,
                filter: "[EsteSters] = 0");
        }
    }
}
