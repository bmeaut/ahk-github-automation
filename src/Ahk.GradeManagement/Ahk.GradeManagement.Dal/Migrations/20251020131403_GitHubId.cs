using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class GitHubId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GitHubId",
                table: "PullRequest",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubId",
                table: "PullRequest");
        }
    }
}
