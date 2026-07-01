using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddComunicareDispozitiePrefect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComunicariDispozitiePrefect",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispozitieId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ComunicariDispozitiePrefect", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunicariDispozitiePrefect_Dispozitii_DispozitieId",
                        column: x => x.DispozitieId,
                        principalTable: "Dispozitii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComunicariDispozitiePrefect_Institutii_InstitutieId",
                        column: x => x.InstitutieId,
                        principalTable: "Institutii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComunicariDispozitiePrefect_DispozitieId",
                table: "ComunicariDispozitiePrefect",
                column: "DispozitieId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicariDispozitiePrefect_InstitutieId_AnRegistru_NumarOrdineInRegistru",
                table: "ComunicariDispozitiePrefect",
                columns: new[] { "InstitutieId", "AnRegistru", "NumarOrdineInRegistru" },
                unique: true,
                filter: "[EsteSters] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComunicariDispozitiePrefect");
        }
    }
}
