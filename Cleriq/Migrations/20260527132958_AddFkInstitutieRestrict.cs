using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddFkInstitutieRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comisii_Institutii_InstitutieId",
                table: "Comisii");

            migrationBuilder.DropForeignKey(
                name: "FK_Consilieri_Institutii_InstitutieId",
                table: "Consilieri");

            migrationBuilder.DropForeignKey(
                name: "FK_Sedinte_Institutii_InstitutieId",
                table: "Sedinte");

            migrationBuilder.CreateIndex(
                name: "IX_Voturi_InstitutieId",
                table: "Voturi",
                column: "InstitutieId");

            migrationBuilder.CreateIndex(
                name: "IX_PuncteOrdineZi_InstitutieId",
                table: "PuncteOrdineZi",
                column: "InstitutieId");

            migrationBuilder.CreateIndex(
                name: "IX_ProceseVerbale_InstitutieId",
                table: "ProceseVerbale",
                column: "InstitutieId");

            migrationBuilder.CreateIndex(
                name: "IX_Prezente_InstitutieId",
                table: "Prezente",
                column: "InstitutieId");

            migrationBuilder.CreateIndex(
                name: "IX_Mandate_InstitutieId",
                table: "Mandate",
                column: "InstitutieId");

            migrationBuilder.CreateIndex(
                name: "IX_Convocari_InstitutieId",
                table: "Convocari",
                column: "InstitutieId");

            migrationBuilder.CreateIndex(
                name: "IX_ComisieMembri_InstitutieId",
                table: "ComisieMembri",
                column: "InstitutieId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComisieMembri_Institutii_InstitutieId",
                table: "ComisieMembri",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comisii_Institutii_InstitutieId",
                table: "Comisii",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Consilieri_Institutii_InstitutieId",
                table: "Consilieri",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Convocari_Institutii_InstitutieId",
                table: "Convocari",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mandate_Institutii_InstitutieId",
                table: "Mandate",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Prezente_Institutii_InstitutieId",
                table: "Prezente",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProceseVerbale_Institutii_InstitutieId",
                table: "ProceseVerbale",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PuncteOrdineZi_Institutii_InstitutieId",
                table: "PuncteOrdineZi",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sedinte_Institutii_InstitutieId",
                table: "Sedinte",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Voturi_Institutii_InstitutieId",
                table: "Voturi",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComisieMembri_Institutii_InstitutieId",
                table: "ComisieMembri");

            migrationBuilder.DropForeignKey(
                name: "FK_Comisii_Institutii_InstitutieId",
                table: "Comisii");

            migrationBuilder.DropForeignKey(
                name: "FK_Consilieri_Institutii_InstitutieId",
                table: "Consilieri");

            migrationBuilder.DropForeignKey(
                name: "FK_Convocari_Institutii_InstitutieId",
                table: "Convocari");

            migrationBuilder.DropForeignKey(
                name: "FK_Mandate_Institutii_InstitutieId",
                table: "Mandate");

            migrationBuilder.DropForeignKey(
                name: "FK_Prezente_Institutii_InstitutieId",
                table: "Prezente");

            migrationBuilder.DropForeignKey(
                name: "FK_ProceseVerbale_Institutii_InstitutieId",
                table: "ProceseVerbale");

            migrationBuilder.DropForeignKey(
                name: "FK_PuncteOrdineZi_Institutii_InstitutieId",
                table: "PuncteOrdineZi");

            migrationBuilder.DropForeignKey(
                name: "FK_Sedinte_Institutii_InstitutieId",
                table: "Sedinte");

            migrationBuilder.DropForeignKey(
                name: "FK_Voturi_Institutii_InstitutieId",
                table: "Voturi");

            migrationBuilder.DropIndex(
                name: "IX_Voturi_InstitutieId",
                table: "Voturi");

            migrationBuilder.DropIndex(
                name: "IX_PuncteOrdineZi_InstitutieId",
                table: "PuncteOrdineZi");

            migrationBuilder.DropIndex(
                name: "IX_ProceseVerbale_InstitutieId",
                table: "ProceseVerbale");

            migrationBuilder.DropIndex(
                name: "IX_Prezente_InstitutieId",
                table: "Prezente");

            migrationBuilder.DropIndex(
                name: "IX_Mandate_InstitutieId",
                table: "Mandate");

            migrationBuilder.DropIndex(
                name: "IX_Convocari_InstitutieId",
                table: "Convocari");

            migrationBuilder.DropIndex(
                name: "IX_ComisieMembri_InstitutieId",
                table: "ComisieMembri");

            migrationBuilder.AddForeignKey(
                name: "FK_Comisii_Institutii_InstitutieId",
                table: "Comisii",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Consilieri_Institutii_InstitutieId",
                table: "Consilieri",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sedinte_Institutii_InstitutieId",
                table: "Sedinte",
                column: "InstitutieId",
                principalTable: "Institutii",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
