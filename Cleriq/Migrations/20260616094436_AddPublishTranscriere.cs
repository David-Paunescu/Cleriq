using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishTranscriere : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContinutPublicat",
                table: "Transcrieri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataPublicare",
                table: "Transcrieri",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublicataDe",
                table: "Transcrieri",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContinutPublicat",
                table: "Transcrieri");

            migrationBuilder.DropColumn(
                name: "DataPublicare",
                table: "Transcrieri");

            migrationBuilder.DropColumn(
                name: "PublicataDe",
                table: "Transcrieri");
        }
    }
}
