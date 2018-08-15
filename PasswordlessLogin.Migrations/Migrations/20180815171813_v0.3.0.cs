using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimpleIAM.PasswordlessLogin.Migrations.Migrations
{
    public partial class v030 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "AuthorizedDevices",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    SubjectId = table.Column<string>(maxLength: 36, nullable: false),
                    DeviceIdHash = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    AddedOn = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizedDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OneTimeCodes",
                schema: "auth",
                columns: table => new
                {
                    SentTo = table.Column<string>(maxLength: 254, nullable: false),
                    ClientNonceHash = table.Column<string>(nullable: true),
                    ShortCode = table.Column<string>(nullable: true),
                    LongCode = table.Column<string>(nullable: true),
                    ExpiresUTC = table.Column<DateTime>(nullable: false),
                    FailedAttemptCount = table.Column<int>(nullable: false),
                    SentCount = table.Column<int>(nullable: false),
                    RedirectUrl = table.Column<string>(maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeCodes", x => x.SentTo);
                });

            migrationBuilder.CreateTable(
                name: "PasswordHashes",
                schema: "auth",
                columns: table => new
                {
                    SubjectId = table.Column<string>(maxLength: 36, nullable: false),
                    Hash = table.Column<string>(nullable: false),
                    LastChangedUTC = table.Column<DateTime>(nullable: false),
                    FailedAttemptCount = table.Column<int>(nullable: false),
                    TempLockUntilUTC = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHashes", x => x.SubjectId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "auth",
                columns: table => new
                {
                    SubjectId = table.Column<string>(maxLength: 36, nullable: false),
                    Email = table.Column<string>(maxLength: 254, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.SubjectId);
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    SubjectId = table.Column<string>(maxLength: 36, nullable: false),
                    Type = table.Column<string>(maxLength: 255, nullable: false),
                    Value = table.Column<string>(maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Claims_Users_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_SubjectId",
                schema: "auth",
                table: "Claims",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Type",
                schema: "auth",
                table: "Claims",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "auth",
                table: "Users",
                column: "Email",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorizedDevices",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Claims",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "OneTimeCodes",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "PasswordHashes",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "auth");
        }
    }
}
