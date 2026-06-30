using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeazaAuditIstoricActiuneAct : Migration
    {
        // Rename DATA-PRESERVING (hand-edited peste drop+create scaffoldat de EF):
        // IstoricActiuniHcl → IstoricActiuniAct, HclId → ActId, + TipAct (backfill = 1 Hcl),
        // scoatem FK-ul către Hcluri (referință slabă). Redenumim și PK/FK/index ca să NU
        // existe drift între snapshot și DB.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IstoricActiuniHcl_Hcluri_HclId",
                table: "IstoricActiuniHcl");

            migrationBuilder.DropIndex(
                name: "IX_IstoricActiuniHcl_HclId",
                table: "IstoricActiuniHcl");

            migrationBuilder.RenameColumn(
                name: "HclId",
                table: "IstoricActiuniHcl",
                newName: "ActId");

            migrationBuilder.AddColumn<int>(
                name: "TipAct",
                table: "IstoricActiuniHcl",
                type: "int",
                nullable: false,
                defaultValue: 1); // backfill rândurile existente la Hcl = 1

            migrationBuilder.RenameIndex(
                name: "IX_IstoricActiuniHcl_InstitutieId",
                table: "IstoricActiuniHcl",
                newName: "IX_IstoricActiuniAct_InstitutieId");

            migrationBuilder.Sql("EXEC sp_rename N'PK_IstoricActiuniHcl', N'PK_IstoricActiuniAct';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_IstoricActiuniHcl_Institutii_InstitutieId', N'FK_IstoricActiuniAct_Institutii_InstitutieId';");

            migrationBuilder.RenameTable(
                name: "IstoricActiuniHcl",
                newName: "IstoricActiuniAct");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "IstoricActiuniAct",
                newName: "IstoricActiuniHcl");

            migrationBuilder.Sql("EXEC sp_rename N'PK_IstoricActiuniAct', N'PK_IstoricActiuniHcl';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_IstoricActiuniAct_Institutii_InstitutieId', N'FK_IstoricActiuniHcl_Institutii_InstitutieId';");

            migrationBuilder.RenameIndex(
                name: "IX_IstoricActiuniAct_InstitutieId",
                table: "IstoricActiuniHcl",
                newName: "IX_IstoricActiuniHcl_InstitutieId");

            migrationBuilder.DropColumn(
                name: "TipAct",
                table: "IstoricActiuniHcl");

            migrationBuilder.RenameColumn(
                name: "ActId",
                table: "IstoricActiuniHcl",
                newName: "HclId");

            migrationBuilder.CreateIndex(
                name: "IX_IstoricActiuniHcl_HclId",
                table: "IstoricActiuniHcl",
                column: "HclId");

            migrationBuilder.AddForeignKey(
                name: "FK_IstoricActiuniHcl_Hcluri_HclId",
                table: "IstoricActiuniHcl",
                column: "HclId",
                principalTable: "Hcluri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
