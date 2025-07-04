using Ahk.GradeManagement.Dal.Entities;
using Ahk.GradeManagement.Dal.Entities.Interfaces;
using Ahk.GradeManagement.Dal.Interceptors;

using Microsoft.EntityFrameworkCore;

using System.Linq.Expressions;

namespace Ahk.GradeManagement.Dal;

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
        RegisterTenantAndSoftDeleteQueryFilters(modelBuilder);
    }

    private void RegisterSoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e =>
                typeof(ISoftDelete).IsAssignableFrom(e.ClrType) &&
                !typeof(ITenant).IsAssignableFrom(e.ClrType));

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
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType) ||
                !typeof(ITenant).IsAssignableFrom(entityType.ClrType))
                continue;

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(BuildTenantFilter(entityType.ClrType));
        }
    }

    private LambdaExpression BuildTenantFilter(Type type)
    {
        // e => ((ITenant)e).TenantId == CurrentTenantId
        var param = Expression.Parameter(type, "e");
        var tenantProperty = Expression.Property(param, nameof(ITenant.SubjectId));
        var tenantIdProperty = Expression.Property(Expression.Constant(this), nameof(SubjectIdValue));

        var filterBody = Expression.Equal(tenantProperty, tenantIdProperty);
        return Expression.Lambda(filterBody, param);
    }

    private void RegisterTenantAndSoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType) ||
                !typeof(ITenant).IsAssignableFrom(entityType.ClrType))
                continue;

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(BuildTenantAndSoftDeleteFilter(entityType.ClrType));
        }
    }

    private LambdaExpression BuildTenantAndSoftDeleteFilter(Type type)
    {
        // e => e.SubjectId == SubjectIdValue && e.IsDeleted == false
        var param = Expression.Parameter(type, "e");

        // Tenant filter condition
        var tenantProperty = Expression.Property(param, nameof(ITenant.SubjectId));
        var tenantIdProperty = Expression.Property(Expression.Constant(this), nameof(SubjectIdValue));
        var tenantFilterBody = Expression.Equal(tenantProperty, tenantIdProperty);

        // Soft delete filter condition
        var softDeleteProperty = Expression.Property(param, nameof(ISoftDelete.IsDeleted));
        var notDeletedCondition = Expression.Equal(softDeleteProperty, Expression.Constant(false));

        var combinedFilter = Expression.AndAlso(tenantFilterBody, notDeletedCondition);

        return Expression.Lambda(combinedFilter, param);
    }
}
