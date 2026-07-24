using Ahk.Web.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Data;

/// <summary>
/// EF Core context backing ASP.NET Identity plus the course-scoped domain model. Applies a global query filter
/// on every <see cref="ICourseScoped"/> entity so a request only sees rows for the resolved current course.
///
/// Note: the filter follows <see cref="ICurrentCourseProvider"/>, which is populated by whichever entry point
/// resolved the course (route segment, webhook payload, or CI token). When no course is resolved the filter
/// matches nothing — callers with no course context (e.g. the one-time importer) must set a provider or use
/// <c>IgnoreQueryFilters()</c>.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    private readonly ICurrentCourseProvider currentCourse;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentCourseProvider currentCourse)
        : base(options)
    {
        this.currentCourse = currentCourse;
    }

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<CourseGitHubConfig> CourseGitHubConfigs => Set<CourseGitHubConfig>();

    public DbSet<CourseMembership> CourseMemberships => Set<CourseMembership>();

    public DbSet<CourseWebhookToken> CourseWebhookTokens => Set<CourseWebhookToken>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<Submission> Submissions => Set<Submission>();

    public DbSet<SubmissionEvent> SubmissionEvents => Set<SubmissionEvent>();

    public DbSet<GradeRecord> GradeRecords => Set<GradeRecord>();

    public DbSet<GradeExercisePoint> GradeExercisePoints => Set<GradeExercisePoint>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.DisplayName).HasMaxLength(256);
            e.Property(u => u.NeptunCode).HasMaxLength(32);
            e.Property(u => u.Affiliation).HasMaxLength(256);

            // Not unique: directory accounts may have no neptun code, and it is a lookup key, not an identity.
            e.HasIndex(u => u.NeptunCode);
        });

        builder.Entity<Course>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.Slug).HasMaxLength(64).IsRequired();
            e.Property(c => c.Name).HasMaxLength(256).IsRequired();
            e.Property(c => c.GitHubOrganization).HasMaxLength(256);
            e.Property(c => c.RepoNamePrefix).HasMaxLength(256);

            // Machine-to-machine course resolution: organization first, then repo-name prefix.
            e.HasIndex(c => c.GitHubOrganization);
            e.HasIndex(c => c.RepoNamePrefix);

            e.HasOne(c => c.GitHubConfig)
                .WithOne(g => g.Course!)
                .HasForeignKey<CourseGitHubConfig>(g => g.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CourseGitHubConfig>(e =>
        {
            e.HasIndex(g => g.CourseId).IsUnique();
            e.Property(g => g.GitHubAppId).HasMaxLength(64);
            e.Property(g => g.GitHubAccessToken).HasMaxLength(512);
            e.Property(g => g.GitHubWebhookSecret).HasMaxLength(512);
        });

        builder.Entity<CourseMembership>(e =>
        {
            e.HasKey(m => new { m.UserId, m.CourseId });
            e.HasOne(m => m.User).WithMany(u => u.CourseMemberships).HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Course).WithMany(c => c.Memberships).HasForeignKey(m => m.CourseId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CourseWebhookToken>(e =>
        {
            e.Property(t => t.Token).HasMaxLength(128).IsRequired();
            e.Property(t => t.Secret).HasMaxLength(512).IsRequired();
            e.Property(t => t.Description).HasMaxLength(512);

            // Globally unique: the CI callback carries no {course} segment, so the token resolves the course.
            e.HasIndex(t => t.Token).IsUnique();
            e.HasOne(t => t.Course).WithMany().HasForeignKey(t => t.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(t => t.CourseId == this.currentCourse.CurrentCourseId);
        });

        builder.Entity<Student>(e =>
        {
            e.Property(s => s.Neptun).HasMaxLength(32).IsRequired();
            e.Property(s => s.GitHubUsername).HasMaxLength(128);
            e.Property(s => s.Name).HasMaxLength(256);

            e.HasIndex(s => new { s.CourseId, s.Neptun }).IsUnique();
            e.HasOne(s => s.Course).WithMany().HasForeignKey(s => s.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(s => s.CourseId == this.currentCourse.CurrentCourseId);
        });

        builder.Entity<Submission>(e =>
        {
            e.Property(s => s.GitHubRepoName).HasMaxLength(400).IsRequired();

            e.HasIndex(s => new { s.CourseId, s.GitHubRepoName }).IsUnique();
            e.HasIndex(s => new { s.CourseId, s.StudentId });

            e.HasOne(s => s.Course).WithMany().HasForeignKey(s => s.CourseId).OnDelete(DeleteBehavior.Cascade);

            // NoAction (not SetNull): Course cascades to both Student and Submission, and SQL Server rejects
            // the resulting multiple cascade paths. Deleting a course still removes both.
            e.HasOne(s => s.Student).WithMany(st => st!.Submissions).HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.NoAction);
            e.HasQueryFilter(s => s.CourseId == this.currentCourse.CurrentCourseId);
        });

        builder.Entity<SubmissionEvent>(e =>
        {
            // Table-per-hierarchy, mirroring the original polymorphic event log.
            e.HasDiscriminator<string>("EventType")
                .HasValue<RepositoryCreatedEvent>(nameof(RepositoryCreatedEvent))
                .HasValue<BranchCreatedEvent>(nameof(BranchCreatedEvent))
                .HasValue<PullRequestEvent>(nameof(PullRequestEvent))
                .HasValue<WorkflowRunEvent>(nameof(WorkflowRunEvent));

            e.Property(x => x.GitHubDeliveryId).HasMaxLength(128);

            e.HasIndex(x => new { x.CourseId, x.SubmissionId, x.Timestamp });

            // Filtered unique index: redelivered webhooks must not duplicate events.
            e.HasIndex(x => x.GitHubDeliveryId).IsUnique().HasFilter("[GitHubDeliveryId] IS NOT NULL");

            e.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Submission).WithMany(s => s!.Events).HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => x.CourseId == this.currentCourse.CurrentCourseId);
        });

        builder.Entity<BranchCreatedEvent>(e => e.Property(x => x.Branch).HasMaxLength(400));
        builder.Entity<WorkflowRunEvent>(e => e.Property(x => x.Conclusion).HasMaxLength(64));
        builder.Entity<PullRequestEvent>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(64);
            e.Property(x => x.HtmlUrl).HasMaxLength(1024);
            e.Property(x => x.Neptun).HasMaxLength(32);
            e.PrimitiveCollection(x => x.Assignees); // JSON column
        });

        builder.Entity<GradeRecord>(e =>
        {
            e.Property(g => g.Neptun).HasMaxLength(32).IsRequired();
            e.Property(g => g.PrUrl).HasMaxLength(1024);
            e.Property(g => g.Actor).HasMaxLength(256);
            e.Property(g => g.Origin).HasMaxLength(1024);

            // "Latest result for this submission/PR" — the GetLastResultOf access path.
            e.HasIndex(g => new { g.CourseId, g.SubmissionId, g.PrNumber, g.Date });

            // Confirmed-grade listing and CSV export.
            e.HasIndex(g => new { g.CourseId, g.Confirmed, g.Date });

            e.HasOne(g => g.Course).WithMany().HasForeignKey(g => g.CourseId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(g => g.Submission).WithMany(s => s!.Grades).HasForeignKey(g => g.SubmissionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(g => g.Student).WithMany().HasForeignKey(g => g.StudentId).OnDelete(DeleteBehavior.NoAction);
            e.HasQueryFilter(g => g.CourseId == this.currentCourse.CurrentCourseId);
        });

        builder.Entity<GradeExercisePoint>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(256).IsRequired();
            e.HasOne(p => p.GradeRecord).WithMany(g => g!.Points).HasForeignKey(p => p.GradeRecordId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
