using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimpleIAM.PasswordlessLogin.SqlServer.Migrations
{
    public partial class v060 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "EventLog",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Time = table.Column<DateTime>(nullable: false),
                    Username = table.Column<string>(maxLength: 254, nullable: true),
                    EventType = table.Column<string>(maxLength: 30, nullable: false),
                    Details = table.Column<string>(maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLog", x => x.Id);
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
                name: "TrustedBrowsers",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<string>(maxLength: 36, nullable: false),
                    BrowserIdHash = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    AddedOn = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustedBrowsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "auth",
                columns: table => new
                {
                    SubjectId = table.Column<string>(maxLength: 36, nullable: false),
                    Email = table.Column<string>(maxLength: 254, nullable: false),
                    CreatedUTC = table.Column<DateTime>(nullable: false)
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
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                name: "Claims",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "EventLog",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "OneTimeCodes",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "PasswordHashes",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "TrustedBrowsers",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "auth");
        }
    }
}
