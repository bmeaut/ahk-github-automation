using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ahk.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class GitHubWebhookDeliveryQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GitHubWebhookDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    DeliveryId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RepositoryFullName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OutcomesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HandlerCount = table.Column<int>(type: "int", nullable: false),
                    FailedHandlerCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubWebhookDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GitHubWebhookDeliveries_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GitHubWebhookDeliveries_CourseId_ReceivedAt",
                table: "GitHubWebhookDeliveries",
                columns: new[] { "CourseId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GitHubWebhookDeliveries_DeliveryId",
                table: "GitHubWebhookDeliveries",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_GitHubWebhookDeliveries_ReceivedAt",
                table: "GitHubWebhookDeliveries",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GitHubWebhookDeliveries_Status_NextAttemptAt_Id",
                table: "GitHubWebhookDeliveries",
                columns: new[] { "Status", "NextAttemptAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GitHubWebhookDeliveries");
        }
    }
}
