using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "PullRequest");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PullRequest",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "PullRequest");

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "PullRequest",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
