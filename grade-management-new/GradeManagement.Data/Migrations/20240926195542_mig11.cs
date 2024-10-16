using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "SubjectTeacher",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SubjectId",
                table: "Score",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SubjectId",
                table: "PullRequest",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SubjectId",
                table: "Group",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SubjectId",
                table: "Exercise",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SubjectId",
                table: "Assignment",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "ScoreTypeExercise",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExerciseId = table.Column<long>(type: "bigint", nullable: false),
                    ScoreTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ScoreId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreTypeExercise", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreTypeExercise_Exercise_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercise",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScoreTypeExercise_ScoreType_ScoreTypeId",
                        column: x => x.ScoreTypeId,
                        principalTable: "ScoreType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScoreTypeExercise_Score_ScoreId",
                        column: x => x.ScoreId,
                        principalTable: "Score",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTypeExercise_ExerciseId",
                table: "ScoreTypeExercise",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTypeExercise_ScoreId",
                table: "ScoreTypeExercise",
                column: "ScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTypeExercise_ScoreTypeId",
                table: "ScoreTypeExercise",
                column: "ScoreTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoreTypeExercise");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "SubjectTeacher");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Score");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "PullRequest");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Group");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Exercise");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Assignment");
        }
    }
}
