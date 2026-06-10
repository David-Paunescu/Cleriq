using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddPvSemnat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaleStocareSemnat",
                table: "ProceseVerbale",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataIncarcareSemnat",
                table: "ProceseVerbale",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashSha256Semnat",
                table: "ProceseVerbale",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MarimeSemnat",
                table: "ProceseVerbale",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeFisierSemnat",
                table: "ProceseVerbale",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaleStocareSemnat",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "DataIncarcareSemnat",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "HashSha256Semnat",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "MarimeSemnat",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "NumeFisierSemnat",
                table: "ProceseVerbale");
        }
    }
}
