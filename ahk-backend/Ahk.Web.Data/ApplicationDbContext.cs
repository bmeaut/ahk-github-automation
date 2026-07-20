using Ahk.Web.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Data;

/// <summary>
/// EF Core context backing ASP.NET Identity plus the course model. Applies a global query filter on every
/// <see cref="ICourseScoped"/> entity so a request only sees rows for the resolved current course.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    private readonly ICurrentCourseProvider currentCourse;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentCourseProvider currentCourse)
        : base(options)
    {
        this.currentCourse = currentCourse;
    }

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<CourseMembership> CourseMemberships => Set<CourseMembership>();

    public DbSet<CourseNote> CourseNotes => Set<CourseNote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Course>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.Slug).HasMaxLength(64).IsRequired();
            e.Property(c => c.Name).HasMaxLength(256).IsRequired();
        });

        builder.Entity<CourseMembership>(e =>
        {
            e.HasKey(m => new { m.UserId, m.CourseId });
            e.HasOne(m => m.User).WithMany(u => u.CourseMemberships).HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Course).WithMany(c => c.Memberships).HasForeignKey(m => m.CourseId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CourseNote>(e =>
        {
            e.Property(n => n.Text).HasMaxLength(1024).IsRequired();
            e.HasOne(n => n.Course).WithMany().HasForeignKey(n => n.CourseId).OnDelete(DeleteBehavior.Cascade);
            // Course-scoping filter: only rows for the request's current course are visible.
            e.HasQueryFilter(n => n.CourseId == this.currentCourse.CurrentCourseId);
        });
    }
}
