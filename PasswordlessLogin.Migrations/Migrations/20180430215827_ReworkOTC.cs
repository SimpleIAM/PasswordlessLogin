using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace SimpleIAM.PasswordlessLogin.Migrations.Migrations
{
    public partial class ReworkOTC : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkCode",
                table: "OneTimeCodes");

            migrationBuilder.DropColumn(
                name: "OTC",
                table: "OneTimeCodes");

            migrationBuilder.RenameColumn(
                name: "FailedAuthenticationCount",
                table: "PasswordHashes",
                newName: "FailedAttemptCount");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "OneTimeCodes",
                newName: "SentTo");

            migrationBuilder.AddColumn<int>(
                name: "FailedAttemptCount",
                table: "OneTimeCodes",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LongCodeHash",
                table: "OneTimeCodes",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortCodeHash",
                table: "OneTimeCodes",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedAttemptCount",
                table: "OneTimeCodes");

            migrationBuilder.DropColumn(
                name: "LongCodeHash",
                table: "OneTimeCodes");

            migrationBuilder.DropColumn(
                name: "ShortCodeHash",
                table: "OneTimeCodes");

            migrationBuilder.RenameColumn(
                name: "FailedAttemptCount",
                table: "PasswordHashes",
                newName: "FailedAuthenticationCount");

            migrationBuilder.RenameColumn(
                name: "SentTo",
                table: "OneTimeCodes",
                newName: "Email");

            migrationBuilder.AddColumn<string>(
                name: "LinkCode",
                table: "OneTimeCodes",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OTC",
                table: "OneTimeCodes",
                maxLength: 8,
                nullable: true);
        }
    }
}
