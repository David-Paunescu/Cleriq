using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddModulHCL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Document_ExactUnContext",
                table: "Documente");

            migrationBuilder.AddColumn<int>(
                name: "PresedinteSedintaConsilierId",
                table: "Sedinte",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HclId",
                table: "Documente",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumarOrdinAnexa",
                table: "Documente",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipDocumentHcl",
                table: "Documente",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Hcluri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numar = table.Column<int>(type: "int", nullable: true),
                    AnNumerotare = table.Column<int>(type: "int", nullable: true),
                    TipHcl = table.Column<int>(type: "int", nullable: false),
                    Titlu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Continut = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataAdoptare = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataIntrareInVigoare = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PunctOrdineZiId = table.Column<int>(type: "int", nullable: false),
                    VotPentru = table.Column<int>(type: "int", nullable: false),
                    VotImpotriva = table.Column<int>(type: "int", nullable: false),
                    VotAbtinere = table.Column<int>(type: "int", nullable: false),
                    TipMajoritate = table.Column<int>(type: "int", nullable: false),
                    CaleStocareSemnat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NumeFisierSemnat = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MarimeSemnat = table.Column<long>(type: "bigint", nullable: true),
                    HashSha256Semnat = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DataIncarcareSemnat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataPublicareMol = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublicataDe = table.Column<int>(type: "int", nullable: true),
                    EstePublicat = table.Column<bool>(type: "bit", nullable: false),
                    MotivLipsaSemnaturaPresedinte = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DataInvalidare = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivInvalidare = table.Column<int>(type: "int", nullable: true),
                    RefInvalidare = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InvalidatDe = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Hcluri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hcluri_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Hcluri_PuncteOrdineZi_PunctOrdineZiId",
                        column: x => x.PunctOrdineZiId,
                        principalTable: "PuncteOrdineZi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComunicariHclPrefect",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HclId = table.Column<int>(type: "int", nullable: false),
                    NumarOrdineInRegistru = table.Column<int>(type: "int", nullable: false),
                    AnRegistru = table.Column<int>(type: "int", nullable: false),
                    DataTrimiteri = table.Column<DateOnly>(type: "date", nullable: false),
                    DataInregistrareInRegistru = table.Column<DateOnly>(type: "date", nullable: false),
                    CanalTransmitere = table.Column<int>(type: "int", nullable: false),
                    NrInregistrarePrefect = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DataConfirmarePrefect = table.Column<DateOnly>(type: "date", nullable: true),
                    ObiectiiMotivate = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RaspunsPrefect = table.Column<int>(type: "int", nullable: true),
                    DataRaspunsPrefect = table.Column<DateOnly>(type: "date", nullable: true),
                    ObservatiiInterne = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_ComunicariHclPrefect", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunicariHclPrefect_Hcluri_HclId",
                        column: x => x.HclId,
                        principalTable: "Hcluri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComunicariHclPrefect_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RelatiiHcl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HclSursaId = table.Column<int>(type: "int", nullable: false),
                    HclTintaId = table.Column<int>(type: "int", nullable: true),
                    TipRelatie = table.Column<int>(type: "int", nullable: false),
                    ReferintaActExternText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_RelatiiHcl", x => x.Id);
                    table.CheckConstraint("CK_RelatieHcl_ExactUnaTinta", "(CASE WHEN [HclTintaId] IS NULL THEN 0 ELSE 1 END +  CASE WHEN [ReferintaActExternText] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_RelatiiHcl_Hcluri_HclSursaId",
                        column: x => x.HclSursaId,
                        principalTable: "Hcluri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RelatiiHcl_Hcluri_HclTintaId",
                        column: x => x.HclTintaId,
                        principalTable: "Hcluri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RelatiiHcl_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SemnatariHcl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HclId = table.Column<int>(type: "int", nullable: false),
                    PersoanaId = table.Column<int>(type: "int", nullable: true),
                    ConsilierId = table.Column<int>(type: "int", nullable: true),
                    RolSemnatar = table.Column<int>(type: "int", nullable: false),
                    DataSemnare = table.Column<DateOnly>(type: "date", nullable: false),
                    OrdineAfisare = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SemnatariHcl", x => x.Id);
                    table.CheckConstraint("CK_SemnatarHcl_ExactUnSubject", "(CASE WHEN [PersoanaId] IS NULL THEN 0 ELSE 1 END +  CASE WHEN [ConsilierId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.CheckConstraint("CK_SemnatarHcl_FkCorectaPerRol", "([RolSemnatar] = 2 AND [PersoanaId] IS NOT NULL AND [ConsilierId] IS NULL) OR ([RolSemnatar] IN (1, 3) AND [ConsilierId] IS NOT NULL AND [PersoanaId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_SemnatariHcl_Consilieri_ConsilierId",
                        column: x => x.ConsilierId,
                        principalTable: "Consilieri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SemnatariHcl_Hcluri_HclId",
                        column: x => x.HclId,
                        principalTable: "Hcluri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SemnatariHcl_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SemnatariHcl_Persoane_PersoanaId",
                        column: x => x.PersoanaId,
                        principalTable: "Persoane",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sedinte_PresedinteSedintaConsilierId",
                table: "Sedinte",
                column: "PresedinteSedintaConsilierId");

            migrationBuilder.CreateIndex(
                name: "IX_Documente_HclId_NumarOrdinAnexa",
                table: "Documente",
                columns: new[] { "HclId", "NumarOrdinAnexa" },
                unique: true,
                filter: "[EsteSters] = 0 AND [TipDocumentHcl] = 1 AND [HclId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Document_AnexaMetadata",
                table: "Documente",
                sql: "[TipDocumentHcl] IS NULL OR [TipDocumentHcl] != 1 OR [NumarOrdinAnexa] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Document_ExactUnContext",
                table: "Documente",
                sql: "(CASE WHEN [SedintaId] IS NULL THEN 0 ELSE 1 END +  CASE WHEN [PunctId] IS NULL THEN 0 ELSE 1 END +  CASE WHEN [HclId] IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicariHclPrefect_HclId",
                table: "ComunicariHclPrefect",
                column: "HclId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicariHclPrefect_InstitutieId_AnRegistru_NumarOrdineInRegistru",
                table: "ComunicariHclPrefect",
                columns: new[] { "InstitutieId", "AnRegistru", "NumarOrdineInRegistru" },
                unique: true,
                filter: "[EsteSters] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Hcluri_InstitutieId_AnNumerotare_Numar",
                table: "Hcluri",
                columns: new[] { "InstitutieId", "AnNumerotare", "Numar" },
                unique: true,
                filter: "[EsteSters] = 0 AND [Numar] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Hcluri_PunctOrdineZiId",
                table: "Hcluri",
                column: "PunctOrdineZiId",
                unique: true,
                filter: "[EsteSters] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RelatiiHcl_HclSursaId_HclTintaId_TipRelatie",
                table: "RelatiiHcl",
                columns: new[] { "HclSursaId", "HclTintaId", "TipRelatie" },
                unique: true,
                filter: "[EsteSters] = 0 AND [HclTintaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RelatiiHcl_HclTintaId",
                table: "RelatiiHcl",
                column: "HclTintaId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatiiHcl_InstitutieId",
                table: "RelatiiHcl",
                column: "InstitutieId");

            migrationBuilder.CreateIndex(
                name: "IX_SemnatarHcl_PresedinteSedintaActiv",
                table: "SemnatariHcl",
                column: "HclId",
                unique: true,
                filter: "[EsteSters] = 0 AND [RolSemnatar] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SemnatarHcl_SecretarUatActiv",
                table: "SemnatariHcl",
                column: "HclId",
                unique: true,
                filter: "[EsteSters] = 0 AND [RolSemnatar] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_SemnatariHcl_ConsilierId",
                table: "SemnatariHcl",
                column: "ConsilierId");

            migrationBuilder.CreateIndex(
                name: "IX_SemnatariHcl_InstitutieId",
                table: "SemnatariHcl",
                column: "InstitutieId");

            migrationBuilder.CreateIndex(
                name: "IX_SemnatariHcl_PersoanaId",
                table: "SemnatariHcl",
                column: "PersoanaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documente_Hcluri_HclId",
                table: "Documente",
                column: "HclId",
                principalTable: "Hcluri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sedinte_Consilieri_PresedinteSedintaConsilierId",
                table: "Sedinte",
                column: "PresedinteSedintaConsilierId",
                principalTable: "Consilieri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documente_Hcluri_HclId",
                table: "Documente");

            migrationBuilder.DropForeignKey(
                name: "FK_Sedinte_Consilieri_PresedinteSedintaConsilierId",
                table: "Sedinte");

            migrationBuilder.DropTable(
                name: "ComunicariHclPrefect");

            migrationBuilder.DropTable(
                name: "RelatiiHcl");

            migrationBuilder.DropTable(
                name: "SemnatariHcl");

            migrationBuilder.DropTable(
                name: "Hcluri");

            migrationBuilder.DropIndex(
                name: "IX_Sedinte_PresedinteSedintaConsilierId",
                table: "Sedinte");

            migrationBuilder.DropIndex(
                name: "IX_Documente_HclId_NumarOrdinAnexa",
                table: "Documente");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Document_AnexaMetadata",
                table: "Documente");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Document_ExactUnContext",
                table: "Documente");

            migrationBuilder.DropColumn(
                name: "PresedinteSedintaConsilierId",
                table: "Sedinte");

            migrationBuilder.DropColumn(
                name: "HclId",
                table: "Documente");

            migrationBuilder.DropColumn(
                name: "NumarOrdinAnexa",
                table: "Documente");

            migrationBuilder.DropColumn(
                name: "TipDocumentHcl",
                table: "Documente");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Document_ExactUnContext",
                table: "Documente",
                sql: "(CASE WHEN [SedintaId] IS NULL THEN 0 ELSE 1 END +  CASE WHEN [PunctId] IS NULL THEN 0 ELSE 1 END) = 1");
        }
    }
}
