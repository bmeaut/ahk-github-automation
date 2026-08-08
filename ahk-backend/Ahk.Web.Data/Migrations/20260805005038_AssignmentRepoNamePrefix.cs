using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ahk.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentRepoNamePrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RepoNamePrefix",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RepoNamePrefix",
                table: "Assignments");
        }
    }
}
