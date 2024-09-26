using GradeManagement.Data.Interceptors;
using GradeManagement.Data.Models;
using GradeManagement.Data.Models.Interfaces;

using Microsoft.EntityFrameworkCore;

using System.Linq.Expressions;

namespace GradeManagement.Data;

public class GradeManagementDbContext(DbContextOptions<GradeManagementDbContext> options)
    : DbContext(options)
{
    //Add Migration: dotnet ef migrations add <MigrationName> --project GradeManagement.Data --startup-project GradeManagement.Server

    public DbSet<Assignment> Assignment { get; set; }
    public DbSet<AssignmentLog> AssignmentLog { get; set; }
    public DbSet<Course> Course { get; set; }
    public DbSet<Exercise> Exercise { get; set; }
    public DbSet<Group> Group { get; set; }
    public DbSet<GroupStudent> GroupStudent { get; set; }
    public DbSet<GroupTeacher> GroupTeacher { get; set; }
    public DbSet<Language> Language { get; set; }
    public DbSet<PullRequest> PullRequest { get; set; }
    public DbSet<Score> Score { get; set; }
    public DbSet<ScoreType> ScoreType { get; set; }
    public DbSet<ScoreTypeExercise> ScoreTypeExercise { get; set; }
    public DbSet<Semester> Semester { get; set; }
    public DbSet<Student> Student { get; set; }
    public DbSet<Subject> Subject { get; set; }
    public DbSet<SubjectTeacher> SubjectTeacher { get; set; }
    public DbSet<User> User { get; set; }

    public long SubjectIdValue { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(new SoftDeleteInterceptor());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        RegisterSoftDeleteQueryFilters(modelBuilder);
        RegisterTenantQueryFilters(modelBuilder);
    }

    private void RegisterSoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(ISoftDelete)
                .IsAssignableFrom(e.ClrType));

        foreach (var entityType in entityTypes)
        {
            var parameter = Expression.Parameter(entityType.ClrType);

            var softDeletableProperty = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var compareExpression =
                Expression.MakeBinary(ExpressionType.Equal, softDeletableProperty, Expression.Constant(false));
            var lambda = Expression.Lambda(compareExpression, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    private void RegisterTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenant).IsAssignableFrom(entityType.ClrType)) continue;

            var param = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(param, nameof(ITenant.SubjectId));
            var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(SubjectIdValue)), param);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
