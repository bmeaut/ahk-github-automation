using GradeManagement.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Data.Data;

public class GradeManagementDbContext : DbContext
{
    //Add Migration: dotnet ef migrations add <MigrationName> --project GradeManagement.Data --startup-project GradeManagement.Server
    public GradeManagementDbContext(DbContextOptions<GradeManagementDbContext> options) : base(options)
    {
    }

    public DbSet<Assignment> Assignment { get; set; }
    public DbSet<AssignmentEvent> AssignmentEvent { get; set; }
    public DbSet<Course> Course { get; set; }
    public DbSet<Exercise> Exercise { get; set; }
    public DbSet<Group> Group { get; set; }
    public DbSet<GroupStudent> GroupStudent { get; set; }
    public DbSet<GroupTeacher> GroupTeacher { get; set; }
    public DbSet<Language> Language { get; set; }
    public DbSet<PullRequest> PullRequest { get; set; }
    public DbSet<Score> Score { get; set; }
    public DbSet<Semester> Semester { get; set; }
    public DbSet<Student> Student { get; set; }
    public DbSet<Subject> Subject { get; set; }
    public DbSet<SubjectTeacher> SubjectTeacher { get; set; }
    public DbSet<User> User { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AssignmentEvent>()
        .HasOne(e => e.Assignment)
        .WithMany(a => a.AssignmentEvents)
        .HasForeignKey(e => e.AssignmentId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GroupTeacher>()
        .HasOne(ct => ct.Group)
        .WithMany(g => g.GroupTeachers)
        .HasForeignKey(ct => ct.GroupId)
        .OnDelete(DeleteBehavior.Restrict);
    }
}
