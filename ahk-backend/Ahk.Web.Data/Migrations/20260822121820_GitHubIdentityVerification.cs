using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ahk.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class GitHubIdentityVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ⚠️ The two unique indexes below fail on a database where two accounts already claim the same
            // GitHub login or id — which is exactly the state they exist to prevent, so it has to be resolved
            // by hand first. Check before applying:
            //
            //   SELECT GitHubUsername, COUNT(*) FROM AspNetUsers
            //   WHERE GitHubUsername IS NOT NULL GROUP BY GitHubUsername HAVING COUNT(*) > 1;
            //   SELECT GitHubUserId, COUNT(*) FROM AspNetUsers
            //   WHERE GitHubUserId IS NOT NULL GROUP BY GitHubUserId HAVING COUNT(*) > 1;
            //
            // Clear the column on whichever account is not the rightful owner (NULL is allowed and repeatable);
            // that person re-enters their own login next time they open an invite link.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GitHubVerifiedAt",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GitHubUserId",
                table: "AspNetUsers",
                column: "GitHubUserId",
                unique: true,
                filter: "[GitHubUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GitHubUsername",
                table: "AspNetUsers",
                column: "GitHubUsername",
                unique: true,
                filter: "[GitHubUsername] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GitHubUserId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GitHubUsername",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GitHubVerifiedAt",
                table: "AspNetUsers");
        }
    }
}
