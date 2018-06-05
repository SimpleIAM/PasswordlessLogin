using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimpleIAM.OpenIdAuthority.Migrations.Migrations
{
    public partial class UserClaims : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.CreateTable(
                name: "Users",
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
                        principalTable: "Users",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_SubjectId",
                table: "Claims",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Type",
                table: "Claims",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Claims");

            migrationBuilder.DropTable(
                name: "Users");

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
    }
}
