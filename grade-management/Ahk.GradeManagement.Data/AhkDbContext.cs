using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ahk.GradeManagement.Data
{
    public class AhkDbContext : DbContext
    {
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Point> Points { get; set; }
        public DbSet<StudentAssignment> StudentAssignments { get; set; }
        public DbSet<StudentGroup> StudentGroups { get; set; }
        public DbSet<StudentSubject> StudentSubjects { get; set; }
        public DbSet<TeacherGroup> TeacherGroups { get; set; }
        public DbSet<TeacherSubject> TeacherSubjects { get; set; }
        public DbSet<WebhookToken> WebhookTokens { get; set; }
        public DbSet<BranchCreateEvent> BranchCreateEvents { get; set; }
        public DbSet<PullRequestEvent> PullRequestEvents { get; set; }
        public DbSet<RepositoryCreateEvent> RepositoryCreateEvents { get; set; }
        public DbSet<WorkflowRunEvent> WorkflowRunEvents { get; set; }
        public AhkDbContext(DbContextOptions<AhkDbContext> options) : base(options) { }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           
        }
    }
}
