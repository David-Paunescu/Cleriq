using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleriq.Migrations
{
    /// <inheritdoc />
    public partial class AddSmtpConfigInstitutie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SmtpEmailFrom",
                table: "Institutii",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpHost",
                table: "Institutii",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpNumeFrom",
                table: "Institutii",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpParolaCriptata",
                table: "Institutii",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpPort",
                table: "Institutii",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpSecuritate",
                table: "Institutii",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SmtpUtilizator",
                table: "Institutii",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmtpEmailFrom",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "SmtpHost",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "SmtpNumeFrom",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "SmtpParolaCriptata",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "SmtpPort",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "SmtpSecuritate",
                table: "Institutii");

            migrationBuilder.DropColumn(
                name: "SmtpUtilizator",
                table: "Institutii");
        }
    }
}
