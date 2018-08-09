using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace SimpleIAM.PasswordlessLogin.Migrations.Migrations
{
    public partial class RenameOTP : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneTimePasswords");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TempLockUntilUTC",
                table: "PasswordHashes",
                nullable: true,
                oldClrType: typeof(DateTime));

            migrationBuilder.CreateTable(
                name: "OneTimeCodes",
                columns: table => new
                {
                    Email = table.Column<string>(maxLength: 254, nullable: false),
                    ExpiresUTC = table.Column<DateTime>(nullable: false),
                    LinkCode = table.Column<string>(maxLength: 36, nullable: true),
                    OTC = table.Column<string>(maxLength: 8, nullable: true),
                    RedirectUrl = table.Column<string>(maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeCodes", x => x.Email);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneTimeCodes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TempLockUntilUTC",
                table: "PasswordHashes",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "OneTimePasswords",
                columns: table => new
                {
                    Email = table.Column<string>(maxLength: 254, nullable: false),
                    ExpiresUTC = table.Column<DateTime>(nullable: false),
                    LinkCode = table.Column<string>(maxLength: 36, nullable: true),
                    OTP = table.Column<string>(maxLength: 8, nullable: true),
                    RedirectUrl = table.Column<string>(maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimePasswords", x => x.Email);
                });
        }
    }
}
