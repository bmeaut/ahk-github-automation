using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ahk.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CourseHealthCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HealthCheckedAt",
                table: "Courses",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HealthStatus",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthSummary",
                table: "Courses",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthCheckedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "HealthStatus",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "HealthSummary",
                table: "Courses");
        }
    }
}
