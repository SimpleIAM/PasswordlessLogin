using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimpleIAM.PasswordlessLogin.Migrations.Migrations
{
    public partial class v050 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUTC",
                schema: "auth",
                table: "Users",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                schema: "auth",
                table: "EventLog",
                maxLength: 254,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 254);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedUTC",
                schema: "auth",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                schema: "auth",
                table: "EventLog",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 254,
                oldNullable: true);
        }
    }
}
