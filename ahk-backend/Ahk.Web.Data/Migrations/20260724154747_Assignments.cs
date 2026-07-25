using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ahk.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Assignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GitHubUserId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubUsername",
                table: "AspNetUsers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    TemplateRepoName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    InviteToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentAcceptances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GitHubRepoName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    RepoUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    GitHubUsername = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    InvitationPending = table.Column<bool>(type: "bit", nullable: false),
                    InvitationId = table.Column<long>(type: "bigint", nullable: true),
                    InvitationSentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentAcceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentAcceptances_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentAcceptances_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentAcceptances_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentAcceptances_AssignmentId_UserId",
                table: "AssignmentAcceptances",
                columns: new[] { "AssignmentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentAcceptances_CourseId_GitHubRepoName",
                table: "AssignmentAcceptances",
                columns: new[] { "CourseId", "GitHubRepoName" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentAcceptances_UserId",
                table: "AssignmentAcceptances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CourseId_ArchivedAt",
                table: "Assignments",
                columns: new[] { "CourseId", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_InviteToken",
                table: "Assignments",
                column: "InviteToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentAcceptances");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropColumn(
                name: "GitHubUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GitHubUsername",
                table: "AspNetUsers");
        }
    }
}
