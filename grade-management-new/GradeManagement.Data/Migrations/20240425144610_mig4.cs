using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignment_Exercise_ExerciseId",
                table: "Assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignment_Student_StudentId",
                table: "Assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentLog_PullRequest_PullRequestId",
                table: "AssignmentLog");

            migrationBuilder.DropForeignKey(
                name: "FK_Course_Language_LanguageId",
                table: "Course");

            migrationBuilder.DropForeignKey(
                name: "FK_Course_Semester_SemesterId",
                table: "Course");

            migrationBuilder.DropForeignKey(
                name: "FK_Course_Subject_SubjectId",
                table: "Course");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercise_Course_CourseId",
                table: "Exercise");

            migrationBuilder.DropForeignKey(
                name: "FK_Group_Course_CourseId",
                table: "Group");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupStudent_Group_GroupId",
                table: "GroupStudent");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupStudent_Student_StudentId",
                table: "GroupStudent");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupTeacher_User_UserId",
                table: "GroupTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK_PullRequest_Assignment_AssignmentId",
                table: "PullRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_PullRequest_User_TeacherId",
                table: "PullRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_Score_PullRequest_PullRequestId",
                table: "Score");

            migrationBuilder.DropForeignKey(
                name: "FK_Score_ScoreType_ScoreTypeId",
                table: "Score");

            migrationBuilder.DropForeignKey(
                name: "FK_SubjectTeacher_Subject_SubjectId",
                table: "SubjectTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK_SubjectTeacher_User_UserId",
                table: "SubjectTeacher");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_Exercise_ExerciseId",
                table: "Assignment",
                column: "ExerciseId",
                principalTable: "Exercise",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_Student_StudentId",
                table: "Assignment",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentLog_PullRequest_PullRequestId",
                table: "AssignmentLog",
                column: "PullRequestId",
                principalTable: "PullRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Course_Language_LanguageId",
                table: "Course",
                column: "LanguageId",
                principalTable: "Language",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Course_Semester_SemesterId",
                table: "Course",
                column: "SemesterId",
                principalTable: "Semester",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Course_Subject_SubjectId",
                table: "Course",
                column: "SubjectId",
                principalTable: "Subject",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercise_Course_CourseId",
                table: "Exercise",
                column: "CourseId",
                principalTable: "Course",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Group_Course_CourseId",
                table: "Group",
                column: "CourseId",
                principalTable: "Course",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupStudent_Group_GroupId",
                table: "GroupStudent",
                column: "GroupId",
                principalTable: "Group",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupStudent_Student_StudentId",
                table: "GroupStudent",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupTeacher_User_UserId",
                table: "GroupTeacher",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PullRequest_Assignment_AssignmentId",
                table: "PullRequest",
                column: "AssignmentId",
                principalTable: "Assignment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PullRequest_User_TeacherId",
                table: "PullRequest",
                column: "TeacherId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Score_PullRequest_PullRequestId",
                table: "Score",
                column: "PullRequestId",
                principalTable: "PullRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Score_ScoreType_ScoreTypeId",
                table: "Score",
                column: "ScoreTypeId",
                principalTable: "ScoreType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectTeacher_Subject_SubjectId",
                table: "SubjectTeacher",
                column: "SubjectId",
                principalTable: "Subject",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectTeacher_User_UserId",
                table: "SubjectTeacher",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignment_Exercise_ExerciseId",
                table: "Assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignment_Student_StudentId",
                table: "Assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentLog_PullRequest_PullRequestId",
                table: "AssignmentLog");

            migrationBuilder.DropForeignKey(
                name: "FK_Course_Language_LanguageId",
                table: "Course");

            migrationBuilder.DropForeignKey(
                name: "FK_Course_Semester_SemesterId",
                table: "Course");

            migrationBuilder.DropForeignKey(
                name: "FK_Course_Subject_SubjectId",
                table: "Course");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercise_Course_CourseId",
                table: "Exercise");

            migrationBuilder.DropForeignKey(
                name: "FK_Group_Course_CourseId",
                table: "Group");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupStudent_Group_GroupId",
                table: "GroupStudent");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupStudent_Student_StudentId",
                table: "GroupStudent");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupTeacher_User_UserId",
                table: "GroupTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK_PullRequest_Assignment_AssignmentId",
                table: "PullRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_PullRequest_User_TeacherId",
                table: "PullRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_Score_PullRequest_PullRequestId",
                table: "Score");

            migrationBuilder.DropForeignKey(
                name: "FK_Score_ScoreType_ScoreTypeId",
                table: "Score");

            migrationBuilder.DropForeignKey(
                name: "FK_SubjectTeacher_Subject_SubjectId",
                table: "SubjectTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK_SubjectTeacher_User_UserId",
                table: "SubjectTeacher");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_Exercise_ExerciseId",
                table: "Assignment",
                column: "ExerciseId",
                principalTable: "Exercise",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_Student_StudentId",
                table: "Assignment",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentLog_PullRequest_PullRequestId",
                table: "AssignmentLog",
                column: "PullRequestId",
                principalTable: "PullRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Course_Language_LanguageId",
                table: "Course",
                column: "LanguageId",
                principalTable: "Language",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Course_Semester_SemesterId",
                table: "Course",
                column: "SemesterId",
                principalTable: "Semester",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Course_Subject_SubjectId",
                table: "Course",
                column: "SubjectId",
                principalTable: "Subject",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercise_Course_CourseId",
                table: "Exercise",
                column: "CourseId",
                principalTable: "Course",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Group_Course_CourseId",
                table: "Group",
                column: "CourseId",
                principalTable: "Course",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupStudent_Group_GroupId",
                table: "GroupStudent",
                column: "GroupId",
                principalTable: "Group",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupStudent_Student_StudentId",
                table: "GroupStudent",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupTeacher_User_UserId",
                table: "GroupTeacher",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PullRequest_Assignment_AssignmentId",
                table: "PullRequest",
                column: "AssignmentId",
                principalTable: "Assignment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PullRequest_User_TeacherId",
                table: "PullRequest",
                column: "TeacherId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Score_PullRequest_PullRequestId",
                table: "Score",
                column: "PullRequestId",
                principalTable: "PullRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Score_ScoreType_ScoreTypeId",
                table: "Score",
                column: "ScoreTypeId",
                principalTable: "ScoreType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectTeacher_Subject_SubjectId",
                table: "SubjectTeacher",
                column: "SubjectId",
                principalTable: "Subject",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectTeacher_User_UserId",
                table: "SubjectTeacher",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
