using GradeManagement.Data.Models;
using Microsoft.EntityFrameworkCore;
using Task = GradeManagement.Data.Models.Task;

namespace GradeManagement.Data.Data;

public class ApplicationDbContext:DbContext
{
    //Add Migration: dotnet ef migrations add <MigrationName> --project GradeManagement.Data --startup-project GradeManagement.Server
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    DbSet<Assignment> Assignment { get; set; }
    DbSet<AssignmentEvent> AssignmentEvent {get; set;}
    DbSet<Course> Course { get; set; }
    DbSet<CourseTeacher> CourseTeacher { get; set; }
    DbSet<Group> Group { get; set; }
    DbSet<GroupStudent> GroupStudent { get; set; }
    DbSet<Language> Language { get; set; }
    DbSet<PullRequest> PullRequest { get; set; }
    DbSet<Score> Score { get; set; }
    DbSet<Semester> Semester { get; set; }
    DbSet<Student> Student { get; set; }
    DbSet<Subject> Subject { get; set; }
    DbSet<Task> Task { get; set; }
    DbSet<Teacher> Teacher { get; set; }
}
