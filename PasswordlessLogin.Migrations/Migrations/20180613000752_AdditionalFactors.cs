using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimpleIAM.PasswordlessLogin.Migrations.Migrations
{
    public partial class AdditionalFactors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShortCodeHash",
                table: "OneTimeCodes",
                newName: "ShortCode");

            migrationBuilder.RenameColumn(
                name: "LongCodeHash",
                table: "OneTimeCodes",
                newName: "LongCode");

            migrationBuilder.AddColumn<string>(
                name: "ClientNonceHash",
                table: "OneTimeCodes",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SentCount",
                table: "OneTimeCodes",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AuthorizedDevices",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    SubjectId = table.Column<string>(maxLength: 36, nullable: false),
                    DeviceIdHash = table.Column<string>(nullable: false),
                    Description = table.Column<string>(maxLength: 100, nullable: true),
                    AddedOn = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizedDevices", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorizedDevices");

            migrationBuilder.DropColumn(
                name: "ClientNonceHash",
                table: "OneTimeCodes");

            migrationBuilder.DropColumn(
                name: "SentCount",
                table: "OneTimeCodes");

            migrationBuilder.RenameColumn(
                name: "ShortCode",
                table: "OneTimeCodes",
                newName: "ShortCodeHash");

            migrationBuilder.RenameColumn(
                name: "LongCode",
                table: "OneTimeCodes",
                newName: "LongCodeHash");
        }
    }
}
