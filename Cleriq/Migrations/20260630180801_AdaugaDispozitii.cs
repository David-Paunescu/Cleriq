using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AdaugaDispozitii : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dispozitii",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numar = table.Column<int>(type: "int", nullable: true),
                    AnNumerotare = table.Column<int>(type: "int", nullable: true),
                    TipDispozitie = table.Column<int>(type: "int", nullable: false),
                    Titlu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Continut = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataEmitere = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataIntrareInVigoare = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CaleStocareSemnat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NumeFisierSemnat = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MarimeSemnat = table.Column<long>(type: "bigint", nullable: true),
                    HashSha256Semnat = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DataIncarcareSemnat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataPublicareMol = table.Column<DateOnly>(type: "date", nullable: true),
                    PublicataDe = table.Column<int>(type: "int", nullable: true),
                    AIntratInCircuit = table.Column<bool>(type: "bit", nullable: false),
                    EstePublicat = table.Column<bool>(type: "bit", nullable: false),
                    ContrasemnaturaRefuzata = table.Column<bool>(type: "bit", nullable: false),
                    ObiectieLegalitateSecretar = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RefuzContrasemnareDe = table.Column<int>(type: "int", nullable: true),
                    DataRefuzContrasemnare = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataInvalidare = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivInvalidare = table.Column<int>(type: "int", nullable: true),
                    RefInvalidare = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MotivInvalidareAltulText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_Dispozitii", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dispozitii_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SemnatariDispozitie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispozitieId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SemnatariDispozitie", x => x.Id);
                    table.CheckConstraint("CK_SemnatarDispozitie_ExactUnSubject", "(CASE WHEN [PersoanaId] IS NULL THEN 0 ELSE 1 END +  CASE WHEN [ConsilierId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.CheckConstraint("CK_SemnatarDispozitie_FkCorectaPerRol", "([RolSemnatar] = 2 AND [PersoanaId] IS NOT NULL AND [ConsilierId] IS NULL) OR ([RolSemnatar] = 1)");
                    table.ForeignKey(
                        name: "FK_SemnatariDispozitie_Consilieri_ConsilierId",
                        column: x => x.ConsilierId,
                        principalTable: "Consilieri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SemnatariDispozitie_Dispozitii_DispozitieId",
                        column: x => x.DispozitieId,
                        principalTable: "Dispozitii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SemnatariDispozitie_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SemnatariDispozitie_Persoane_PersoanaId",
                        column: x => x.PersoanaId,
                        principalTable: "Persoane",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dispozitii_InstitutieId_AnNumerotare_Numar",
                table: "Dispozitii",
                columns: new[] { "InstitutieId", "AnNumerotare", "Numar" },
                unique: true,
                filter: "[EsteSters] = 0 AND [Numar] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SemnatarDispozitie_EmitentActiv",
                table: "SemnatariDispozitie",
                column: "DispozitieId",
                unique: true,
                filter: "[EsteSters] = 0 AND [RolSemnatar] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SemnatarDispozitie_SecretarContrasemnaturaActiv",
                table: "SemnatariDispozitie",
                column: "DispozitieId",
                unique: true,
                filter: "[EsteSters] = 0 AND [RolSemnatar] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_SemnatariDispozitie_ConsilierId",
                table: "SemnatariDispozitie",
                column: "ConsilierId");

            migrationBuilder.CreateIndex(
                name: "IX_SemnatariDispozitie_InstitutieId",
                table: "SemnatariDispozitie",
                column: "InstitutieId");

            migrationBuilder.CreateIndex(
                name: "IX_SemnatariDispozitie_PersoanaId",
                table: "SemnatariDispozitie",
                column: "PersoanaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SemnatariDispozitie");

            migrationBuilder.DropTable(
                name: "Dispozitii");
        }
    }
}
