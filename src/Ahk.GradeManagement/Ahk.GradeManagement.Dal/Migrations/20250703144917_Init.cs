using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeManagement.Data.Migrations;

/// <inheritdoc />
public partial class Init : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Language",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Language", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ScoreType",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ScoreType", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Semester",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Semester", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Student",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                NeptunCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                GithubId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                MoodleId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Student", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Subject",
            columns: table => new
            {
                SubjectId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                NeptunCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                GitHubOrgName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                CiApiKey = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Subject", x => x.SubjectId);
            });

        migrationBuilder.CreateTable(
            name: "User",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                NeptunCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                GithubId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                BmeEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Type = table.Column<int>(type: "int", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_User", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Course",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                MoodleClientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PublicKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SemesterId = table.Column<long>(type: "bigint", nullable: false),
                SubjectId = table.Column<long>(type: "bigint", nullable: false),
                LanguageId = table.Column<long>(type: "bigint", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Course", x => x.Id);
                table.ForeignKey(
                    name: "FK_Course_Language_LanguageId",
                    column: x => x.LanguageId,
                    principalTable: "Language",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Course_Semester_SemesterId",
                    column: x => x.SemesterId,
                    principalTable: "Semester",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Course_Subject_SubjectId",
                    column: x => x.SubjectId,
                    principalTable: "Subject",
                    principalColumn: "SubjectId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SubjectTeacher",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SubjectId = table.Column<long>(type: "bigint", nullable: false),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                Role = table.Column<int>(type: "int", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SubjectTeacher", x => x.Id);
                table.ForeignKey(
                    name: "FK_SubjectTeacher_Subject_SubjectId",
                    column: x => x.SubjectId,
                    principalTable: "Subject",
                    principalColumn: "SubjectId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SubjectTeacher_User_UserId",
                    column: x => x.UserId,
                    principalTable: "User",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Exercise",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                GithubPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                MoodleScoreUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                MoodleScoreNamePrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ClassroomUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CourseId = table.Column<long>(type: "bigint", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                SubjectId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Exercise", x => x.Id);
                table.ForeignKey(
                    name: "FK_Exercise_Course_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Course",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Group",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CourseId = table.Column<long>(type: "bigint", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                SubjectId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Group", x => x.Id);
                table.ForeignKey(
                    name: "FK_Group_Course_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Course",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Assignment",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                GithubRepoName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                GithubRepoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                StudentId = table.Column<long>(type: "bigint", nullable: false),
                ExerciseId = table.Column<long>(type: "bigint", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                SubjectId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Assignment", x => x.Id);
                table.ForeignKey(
                    name: "FK_Assignment_Exercise_ExerciseId",
                    column: x => x.ExerciseId,
                    principalTable: "Exercise",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Assignment_Student_StudentId",
                    column: x => x.StudentId,
                    principalTable: "Student",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ScoreTypeExercise",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ExerciseId = table.Column<long>(type: "bigint", nullable: false),
                ScoreTypeId = table.Column<long>(type: "bigint", nullable: false),
                Order = table.Column<int>(type: "int", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
            });

        migrationBuilder.CreateTable(
            name: "GroupStudent",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                GroupId = table.Column<long>(type: "bigint", nullable: false),
                StudentId = table.Column<long>(type: "bigint", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GroupStudent", x => x.Id);
                table.ForeignKey(
                    name: "FK_GroupStudent_Group_GroupId",
                    column: x => x.GroupId,
                    principalTable: "Group",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_GroupStudent_Student_StudentId",
                    column: x => x.StudentId,
                    principalTable: "Student",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "GroupTeacher",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                GroupId = table.Column<long>(type: "bigint", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GroupTeacher", x => x.Id);
                table.ForeignKey(
                    name: "FK_GroupTeacher_Group_GroupId",
                    column: x => x.GroupId,
                    principalTable: "Group",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_GroupTeacher_User_UserId",
                    column: x => x.UserId,
                    principalTable: "User",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PullRequest",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                OpeningDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                BranchName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                AssignmentId = table.Column<long>(type: "bigint", nullable: false),
                TeacherId = table.Column<long>(type: "bigint", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                SubjectId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PullRequest", x => x.Id);
                table.ForeignKey(
                    name: "FK_PullRequest_Assignment_AssignmentId",
                    column: x => x.AssignmentId,
                    principalTable: "Assignment",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PullRequest_User_TeacherId",
                    column: x => x.TeacherId,
                    principalTable: "User",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AssignmentLog",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                EventType = table.Column<int>(type: "int", nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                AssignmentId = table.Column<long>(type: "bigint", nullable: false),
                PullRequestId = table.Column<long>(type: "bigint", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AssignmentLog", x => x.Id);
                table.ForeignKey(
                    name: "FK_AssignmentLog_Assignment_AssignmentId",
                    column: x => x.AssignmentId,
                    principalTable: "Assignment",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AssignmentLog_PullRequest_PullRequestId",
                    column: x => x.PullRequestId,
                    principalTable: "PullRequest",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Score",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Value = table.Column<double>(type: "float", nullable: false),
                IsApproved = table.Column<bool>(type: "bit", nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ScoreTypeId = table.Column<long>(type: "bigint", nullable: false),
                PullRequestId = table.Column<long>(type: "bigint", nullable: false),
                TeacherId = table.Column<long>(type: "bigint", nullable: true),
                SyncedToMoodle = table.Column<bool>(type: "bit", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                SubjectId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Score", x => x.Id);
                table.ForeignKey(
                    name: "FK_Score_PullRequest_PullRequestId",
                    column: x => x.PullRequestId,
                    principalTable: "PullRequest",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Score_ScoreType_ScoreTypeId",
                    column: x => x.ScoreTypeId,
                    principalTable: "ScoreType",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Score_User_TeacherId",
                    column: x => x.TeacherId,
                    principalTable: "User",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Assignment_ExerciseId",
            table: "Assignment",
            column: "ExerciseId");

        migrationBuilder.CreateIndex(
            name: "IX_Assignment_StudentId",
            table: "Assignment",
            column: "StudentId");

        migrationBuilder.CreateIndex(
            name: "IX_AssignmentLog_AssignmentId",
            table: "AssignmentLog",
            column: "AssignmentId");

        migrationBuilder.CreateIndex(
            name: "IX_AssignmentLog_PullRequestId",
            table: "AssignmentLog",
            column: "PullRequestId");

        migrationBuilder.CreateIndex(
            name: "IX_Course_LanguageId",
            table: "Course",
            column: "LanguageId");

        migrationBuilder.CreateIndex(
            name: "IX_Course_SemesterId",
            table: "Course",
            column: "SemesterId");

        migrationBuilder.CreateIndex(
            name: "IX_Course_SubjectId",
            table: "Course",
            column: "SubjectId");

        migrationBuilder.CreateIndex(
            name: "IX_Exercise_CourseId",
            table: "Exercise",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_Group_CourseId",
            table: "Group",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupStudent_GroupId",
            table: "GroupStudent",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupStudent_StudentId",
            table: "GroupStudent",
            column: "StudentId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupTeacher_GroupId",
            table: "GroupTeacher",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupTeacher_UserId",
            table: "GroupTeacher",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_PullRequest_AssignmentId",
            table: "PullRequest",
            column: "AssignmentId");

        migrationBuilder.CreateIndex(
            name: "IX_PullRequest_TeacherId",
            table: "PullRequest",
            column: "TeacherId");

        migrationBuilder.CreateIndex(
            name: "IX_Score_PullRequestId",
            table: "Score",
            column: "PullRequestId");

        migrationBuilder.CreateIndex(
            name: "IX_Score_ScoreTypeId",
            table: "Score",
            column: "ScoreTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_Score_TeacherId",
            table: "Score",
            column: "TeacherId");

        migrationBuilder.CreateIndex(
            name: "IX_ScoreTypeExercise_ExerciseId",
            table: "ScoreTypeExercise",
            column: "ExerciseId");

        migrationBuilder.CreateIndex(
            name: "IX_ScoreTypeExercise_ScoreTypeId",
            table: "ScoreTypeExercise",
            column: "ScoreTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_SubjectTeacher_SubjectId",
            table: "SubjectTeacher",
            column: "SubjectId");

        migrationBuilder.CreateIndex(
            name: "IX_SubjectTeacher_UserId",
            table: "SubjectTeacher",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AssignmentLog");

        migrationBuilder.DropTable(
            name: "GroupStudent");

        migrationBuilder.DropTable(
            name: "GroupTeacher");

        migrationBuilder.DropTable(
            name: "Score");

        migrationBuilder.DropTable(
            name: "ScoreTypeExercise");

        migrationBuilder.DropTable(
            name: "SubjectTeacher");

        migrationBuilder.DropTable(
            name: "Group");

        migrationBuilder.DropTable(
            name: "PullRequest");

        migrationBuilder.DropTable(
            name: "ScoreType");

        migrationBuilder.DropTable(
            name: "Assignment");

        migrationBuilder.DropTable(
            name: "User");

        migrationBuilder.DropTable(
            name: "Exercise");

        migrationBuilder.DropTable(
            name: "Student");

        migrationBuilder.DropTable(
            name: "Course");

        migrationBuilder.DropTable(
            name: "Language");

        migrationBuilder.DropTable(
            name: "Semester");

        migrationBuilder.DropTable(
            name: "Subject");
    }
}
