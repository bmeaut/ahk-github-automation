﻿// <auto-generated />
using System;
using GradeManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GradeManagement.Data.Migrations
{
    [DbContext(typeof(GradeManagementDbContext))]
    [Migration("20241024212642_20241024")]
    partial class _20241024
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("GradeManagement.Data.Models.Assignment", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("ExerciseId")
                        .HasColumnType("bigint");

                    b.Property<string>("GithubRepoName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GithubRepoUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<long>("StudentId")
                        .HasColumnType("bigint");

                    b.Property<long>("SubjectId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ExerciseId");

                    b.HasIndex("StudentId");

                    b.ToTable("Assignment");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.AssignmentLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("AssignmentId")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("EventType")
                        .HasColumnType("int");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<long?>("PullRequestId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("AssignmentId");

                    b.HasIndex("PullRequestId");

                    b.ToTable("AssignmentLog");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Course", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<long>("LanguageId")
                        .HasColumnType("bigint");

                    b.Property<string>("MoodleCourseId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("SemesterId")
                        .HasColumnType("bigint");

                    b.Property<long>("SubjectId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("LanguageId");

                    b.HasIndex("SemesterId");

                    b.HasIndex("SubjectId");

                    b.ToTable("Course");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Exercise", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("CourseId")
                        .HasColumnType("bigint");

                    b.Property<string>("GithubPrefix")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("SubjectId")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("dueDate")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.HasIndex("CourseId");

                    b.ToTable("Exercise");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Group", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("CourseId")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("SubjectId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("CourseId");

                    b.ToTable("Group");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.GroupStudent", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("GroupId")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<long>("StudentId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("GroupId");

                    b.HasIndex("StudentId");

                    b.ToTable("GroupStudent");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.GroupTeacher", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("GroupId")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("GroupId");

                    b.HasIndex("UserId");

                    b.ToTable("GroupTeacher");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Language", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Language");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.PullRequest", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("AssignmentId")
                        .HasColumnType("bigint");

                    b.Property<string>("BranchName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset>("OpeningDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<long>("SubjectId")
                        .HasColumnType("bigint");

                    b.Property<long?>("TeacherId")
                        .HasColumnType("bigint");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("AssignmentId");

                    b.HasIndex("TeacherId");

                    b.ToTable("PullRequest");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Score", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<DateTimeOffset>("CreatedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool>("IsApproved")
                        .HasColumnType("bit");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<long>("PullRequestId")
                        .HasColumnType("bigint");

                    b.Property<long>("ScoreTypeId")
                        .HasColumnType("bigint");

                    b.Property<long>("SubjectId")
                        .HasColumnType("bigint");

                    b.Property<long?>("TeacherId")
                        .HasColumnType("bigint");

                    b.Property<double>("Value")
                        .HasColumnType("float");

                    b.HasKey("Id");

                    b.HasIndex("PullRequestId");

                    b.HasIndex("ScoreTypeId");

                    b.HasIndex("TeacherId");

                    b.ToTable("Score");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.ScoreType", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("ScoreType");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.ScoreTypeExercise", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("ExerciseId")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<int>("Order")
                        .HasColumnType("int");

                    b.Property<long?>("ScoreId")
                        .HasColumnType("bigint");

                    b.Property<long>("ScoreTypeId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ExerciseId");

                    b.HasIndex("ScoreId");

                    b.HasIndex("ScoreTypeId");

                    b.ToTable("ScoreTypeExercise");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Semester", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Semester");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Student", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("GithubId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NeptunCode")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Student");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Subject", b =>
                {
                    b.Property<long>("SubjectId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("SubjectId"));

                    b.Property<string>("CiApiKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GitHubOrgName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NeptunCode")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("SubjectId");

                    b.ToTable("Subject");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.SubjectTeacher", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<int>("Role")
                        .HasColumnType("int");

                    b.Property<long>("SubjectId")
                        .HasColumnType("bigint");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("SubjectId");

                    b.HasIndex("UserId");

                    b.ToTable("SubjectTeacher");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("BmeEmail")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GithubId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NeptunCode")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("User");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Assignment", b =>
                {
                    b.HasOne("GradeManagement.Data.Models.Exercise", "Exercise")
                        .WithMany("Assignments")
                        .HasForeignKey("ExerciseId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("GradeManagement.Data.Models.Student", "Student")
                        .WithMany("Assignments")
                        .HasForeignKey("StudentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Exercise");

                    b.Navigation("Student");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.AssignmentLog", b =>
                {
                    b.HasOne("GradeManagement.Data.Models.Assignment", "Assignment")
                        .WithMany("AssignmentLogs")
                        .HasForeignKey("AssignmentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("GradeManagement.Data.Models.PullRequest", "PullRequest")
                        .WithMany("AssignmentLogs")
                        .HasForeignKey("PullRequestId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Assignment");

                    b.Navigation("PullRequest");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Course", b =>
                {
                    b.HasOne("GradeManagement.Data.Models.Language", "Language")
                        .WithMany("Courses")
                        .HasForeignKey("LanguageId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("GradeManagement.Data.Models.Semester", "Semester")
                        .WithMany("Courses")
                        .HasForeignKey("SemesterId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("GradeManagement.Data.Models.Subject", "Subject")
                        .WithMany("Courses")
                        .HasForeignKey("SubjectId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Language");

                    b.Navigation("Semester");

                    b.Navigation("Subject");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Exercise", b =>
                {
                    b.HasOne("GradeManagement.Data.Models.Course", "Course")
                        .WithMany("Exercises")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Course");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Group", b =>
                {
                    b.HasOne("GradeManagement.Data.Models.Course", "Course")
                        .WithMany("Groups")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Course");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.GroupStudent", b =>
                {
                    b.HasOne("GradeManagement.Data.Models.Group", "Group")
                        .WithMany("GroupStudents")
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("GradeManagement.Data.Models.Student", "Student")
                        .WithMany("GroupStudents")
                        .HasForeignKey("StudentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Group");

                    b.Navigation("Student");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.GroupTeacher", b =>
                {
                    b.HasOne("GradeManagement.Data.Models.Group", "Group")
                        .WithMany("GroupTeachers")
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("GradeManagement.Data.Models.User", "User")
                        .WithMany("GroupTeachers")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Group");

                    b.Navigation("User");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.PullRequest", b =>
                {
                    b.HasOne("GradeManagement.Data.Models.Assignment", "Assignment")
                        .WithMany("PullRequests")
                        .HasForeignKey("AssignmentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("GradeManagement.Data.Models.User", "Teacher")
                        .WithMany("PullRequests")
                        .HasForeignKey("TeacherId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Assignment");

                    b.Navigation("Teacher");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Score", b =>
                {
                    b.HasOne("GradeManagement.Data.Models.PullRequest", "PullRequest")
                        .WithMany("Scores")
                        .HasForeignKey("PullRequestId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("GradeManagement.Data.Models.ScoreType", "ScoreType")
                        .WithMany("Scores")
                        .HasForeignKey("ScoreTypeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("GradeManagement.Data.Models.User", "Teacher")
                        .WithMany("Scores")
                        .HasForeignKey("TeacherId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("PullRequest");

                    b.Navigation("ScoreType");

                    b.Navigation("Teacher");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.ScoreTypeExercise", b =>
                {
                    b.HasOne("GradeManagement.Data.Models.Exercise", "Exercise")
                        .WithMany("ScoreTypeExercises")
                        .HasForeignKey("ExerciseId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("GradeManagement.Data.Models.Score", null)
                        .WithMany("ScoreTypeExercises")
                        .HasForeignKey("ScoreId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("GradeManagement.Data.Models.ScoreType", "ScoreType")
                        .WithMany()
                        .HasForeignKey("ScoreTypeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Exercise");

                    b.Navigation("ScoreType");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.SubjectTeacher", b =>
                {
                    b.HasOne("GradeManagement.Data.Models.Subject", "Subject")
                        .WithMany("SubjectTeachers")
                        .HasForeignKey("SubjectId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("GradeManagement.Data.Models.User", "User")
                        .WithMany("SubjectTeachers")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Subject");

                    b.Navigation("User");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Assignment", b =>
                {
                    b.Navigation("AssignmentLogs");

                    b.Navigation("PullRequests");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Course", b =>
                {
                    b.Navigation("Exercises");

                    b.Navigation("Groups");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Exercise", b =>
                {
                    b.Navigation("Assignments");

                    b.Navigation("ScoreTypeExercises");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Group", b =>
                {
                    b.Navigation("GroupStudents");

                    b.Navigation("GroupTeachers");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Language", b =>
                {
                    b.Navigation("Courses");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.PullRequest", b =>
                {
                    b.Navigation("AssignmentLogs");

                    b.Navigation("Scores");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Score", b =>
                {
                    b.Navigation("ScoreTypeExercises");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.ScoreType", b =>
                {
                    b.Navigation("Scores");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Semester", b =>
                {
                    b.Navigation("Courses");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Student", b =>
                {
                    b.Navigation("Assignments");

                    b.Navigation("GroupStudents");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.Subject", b =>
                {
                    b.Navigation("Courses");

                    b.Navigation("SubjectTeachers");
                });

            modelBuilder.Entity("GradeManagement.Data.Models.User", b =>
                {
                    b.Navigation("GroupTeachers");

                    b.Navigation("PullRequests");

                    b.Navigation("Scores");

                    b.Navigation("SubjectTeachers");
                });
#pragma warning restore 612, 618
        }
    }
}
