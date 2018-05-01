using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace SimpleIAM.OpenIdAuthority.Migrations.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "PasswordHashes",
                columns: table => new
                {
                    SubjectId = table.Column<string>(maxLength: 36, nullable: false),
                    FailedAuthenticationCount = table.Column<int>(nullable: false),
                    Hash = table.Column<string>(nullable: false),
                    LastChangedUTC = table.Column<DateTime>(nullable: false),
                    TempLockUntilUTC = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHashes", x => x.SubjectId);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<string>(maxLength: 36, nullable: false),
                    Email = table.Column<string>(maxLength: 254, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Email",
                table: "Subjects",
                column: "Email",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneTimePasswords");

            migrationBuilder.DropTable(
                name: "PasswordHashes");

            migrationBuilder.DropTable(
                name: "Subjects");
        }
    }
}
