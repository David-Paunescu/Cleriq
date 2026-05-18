using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatDe",
                table: "Voturi",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatLa",
                table: "Voturi",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EsteSters",
                table: "Voturi",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModificatDe",
                table: "Voturi",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificatLa",
                table: "Voturi",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Voturi",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StersDe",
                table: "Voturi",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StersLa",
                table: "Voturi",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatDe",
                table: "Sedinte",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatLa",
                table: "Sedinte",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EsteSters",
                table: "Sedinte",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModificatDe",
                table: "Sedinte",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificatLa",
                table: "Sedinte",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Sedinte",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StersDe",
                table: "Sedinte",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StersLa",
                table: "Sedinte",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatDe",
                table: "PuncteOrdineZi",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatLa",
                table: "PuncteOrdineZi",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EsteSters",
                table: "PuncteOrdineZi",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModificatDe",
                table: "PuncteOrdineZi",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificatLa",
                table: "PuncteOrdineZi",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PuncteOrdineZi",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StersDe",
                table: "PuncteOrdineZi",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StersLa",
                table: "PuncteOrdineZi",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatDe",
                table: "ProceseVerbale",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatLa",
                table: "ProceseVerbale",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EsteSters",
                table: "ProceseVerbale",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModificatDe",
                table: "ProceseVerbale",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificatLa",
                table: "ProceseVerbale",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProceseVerbale",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StersDe",
                table: "ProceseVerbale",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StersLa",
                table: "ProceseVerbale",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatDe",
                table: "Prezente",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatLa",
                table: "Prezente",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EsteSters",
                table: "Prezente",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModificatDe",
                table: "Prezente",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificatLa",
                table: "Prezente",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Prezente",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StersDe",
                table: "Prezente",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StersLa",
                table: "Prezente",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatDe",
                table: "Mandate",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatLa",
                table: "Mandate",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EsteSters",
                table: "Mandate",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModificatDe",
                table: "Mandate",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificatLa",
                table: "Mandate",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Mandate",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StersDe",
                table: "Mandate",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StersLa",
                table: "Mandate",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatDe",
                table: "Institutii",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatLa",
                table: "Institutii",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EsteSters",
                table: "Institutii",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModificatDe",
                table: "Institutii",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificatLa",
                table: "Institutii",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Institutii",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StersDe",
                table: "Institutii",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StersLa",
                table: "Institutii",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatDe",
                table: "Consilieri",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatLa",
                table: "Consilieri",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EsteSters",
                table: "Consilieri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModificatDe",
                table: "Consilieri",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificatLa",
                table: "Consilieri",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Consilieri",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StersDe",
                table: "Consilieri",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StersLa",
                table: "Consilieri",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatDe",
                table: "Comisii",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatLa",
                table: "Comisii",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EsteSters",
                table: "Comisii",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModificatDe",
                table: "Comisii",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificatLa",
                table: "Comisii",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Comisii",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StersDe",
                table: "Comisii",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StersLa",
                table: "Comisii",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatDe",
                table: "ComisieMembri",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatLa",
                table: "ComisieMembri",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EsteSters",
                table: "ComisieMembri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModificatDe",
                table: "ComisieMembri",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificatLa",
                table: "ComisieMembri",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ComisieMembri",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StersDe",
                table: "ComisieMembri",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StersLa",
                table: "ComisieMembri",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatDe",
                table: "Voturi");

            migrationBuilder.DropColumn(
                name: "CreatLa",
                table: "Voturi");

            migrationBuilder.DropColumn(
                name: "EsteSters",
                table: "Voturi");

            migrationBuilder.DropColumn(
                name: "ModificatDe",
                table: "Voturi");

            migrationBuilder.DropColumn(
                name: "ModificatLa",
                table: "Voturi");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Voturi");

            migrationBuilder.DropColumn(
                name: "StersDe",
                table: "Voturi");

            migrationBuilder.DropColumn(
                name: "StersLa",
                table: "Voturi");

            migrationBuilder.DropColumn(
                name: "CreatDe",
                table: "Sedinte");

            migrationBuilder.DropColumn(
                name: "CreatLa",
                table: "Sedinte");

            migrationBuilder.DropColumn(
                name: "EsteSters",
                table: "Sedinte");

            migrationBuilder.DropColumn(
                name: "ModificatDe",
                table: "Sedinte");

            migrationBuilder.DropColumn(
                name: "ModificatLa",
                table: "Sedinte");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Sedinte");

            migrationBuilder.DropColumn(
                name: "StersDe",
                table: "Sedinte");

            migrationBuilder.DropColumn(
                name: "StersLa",
                table: "Sedinte");

            migrationBuilder.DropColumn(
                name: "CreatDe",
                table: "PuncteOrdineZi");

            migrationBuilder.DropColumn(
                name: "CreatLa",
                table: "PuncteOrdineZi");

            migrationBuilder.DropColumn(
                name: "EsteSters",
                table: "PuncteOrdineZi");

            migrationBuilder.DropColumn(
                name: "ModificatDe",
                table: "PuncteOrdineZi");

            migrationBuilder.DropColumn(
                name: "ModificatLa",
                table: "PuncteOrdineZi");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PuncteOrdineZi");

            migrationBuilder.DropColumn(
                name: "StersDe",
                table: "PuncteOrdineZi");

            migrationBuilder.DropColumn(
                name: "StersLa",
                table: "PuncteOrdineZi");

            migrationBuilder.DropColumn(
                name: "CreatDe",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "CreatLa",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "EsteSters",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "ModificatDe",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "ModificatLa",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "StersDe",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "StersLa",
                table: "ProceseVerbale");

            migrationBuilder.DropColumn(
                name: "CreatDe",
                table: "Prezente");

            migrationBuilder.DropColumn(
                name: "CreatLa",
                table: "Prezente");

            migrationBuilder.DropColumn(
                name: "EsteSters",
                table: "Prezente");

            migrationBuilder.DropColumn(
                name: "ModificatDe",
                table: "Prezente");

            migrationBuilder.DropColumn(
                name: "ModificatLa",
                table: "Prezente");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Prezente");

            migrationBuilder.DropColumn(
                name: "StersDe",
                table: "Prezente");

            migrationBuilder.DropColumn(
                name: "StersLa",
                table: "Prezente");

            migrationBuilder.DropColumn(
                name: "CreatDe",
                table: "Mandate");

            migrationBuilder.DropColumn(
                name: "CreatLa",
                table: "Mandate");

            migrationBuilder.DropColumn(
                name: "EsteSters",
                table: "Mandate");

            migrationBuilder.DropColumn(
                name: "ModificatDe",
                table: "Mandate");

            migrationBuilder.DropColumn(
                name: "ModificatLa",
                table: "Mandate");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Mandate");

            migrationBuilder.DropColumn(
                name: "StersDe",
                table: "Mandate");

            migrationBuilder.DropColumn(
                name: "StersLa",
                table: "Mandate");

            migrationBuilder.DropColumn(
                name: "CreatDe",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "CreatLa",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "EsteSters",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "ModificatDe",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "ModificatLa",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "StersDe",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "StersLa",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "CreatDe",
                table: "Consilieri");

            migrationBuilder.DropColumn(
                name: "CreatLa",
                table: "Consilieri");

            migrationBuilder.DropColumn(
                name: "EsteSters",
                table: "Consilieri");

            migrationBuilder.DropColumn(
                name: "ModificatDe",
                table: "Consilieri");

            migrationBuilder.DropColumn(
                name: "ModificatLa",
                table: "Consilieri");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Consilieri");

            migrationBuilder.DropColumn(
                name: "StersDe",
                table: "Consilieri");

            migrationBuilder.DropColumn(
                name: "StersLa",
                table: "Consilieri");

            migrationBuilder.DropColumn(
                name: "CreatDe",
                table: "Comisii");

            migrationBuilder.DropColumn(
                name: "CreatLa",
                table: "Comisii");

            migrationBuilder.DropColumn(
                name: "EsteSters",
                table: "Comisii");

            migrationBuilder.DropColumn(
                name: "ModificatDe",
                table: "Comisii");

            migrationBuilder.DropColumn(
                name: "ModificatLa",
                table: "Comisii");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Comisii");

            migrationBuilder.DropColumn(
                name: "StersDe",
                table: "Comisii");

            migrationBuilder.DropColumn(
                name: "StersLa",
                table: "Comisii");

            migrationBuilder.DropColumn(
                name: "CreatDe",
                table: "ComisieMembri");

            migrationBuilder.DropColumn(
                name: "CreatLa",
                table: "ComisieMembri");

            migrationBuilder.DropColumn(
                name: "EsteSters",
                table: "ComisieMembri");

            migrationBuilder.DropColumn(
                name: "ModificatDe",
                table: "ComisieMembri");

            migrationBuilder.DropColumn(
                name: "ModificatLa",
                table: "ComisieMembri");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ComisieMembri");

            migrationBuilder.DropColumn(
                name: "StersDe",
                table: "ComisieMembri");

            migrationBuilder.DropColumn(
                name: "StersLa",
                table: "ComisieMembri");
        }
    }
}
