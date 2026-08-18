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

    public DbSet<GitHubWebhookDelivery> GitHubWebhookDeliveries => Set<GitHubWebhookDelivery>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<Submission> Submissions => Set<Submission>();

    public DbSet<SubmissionEvent> SubmissionEvents => Set<SubmissionEvent>();

    public DbSet<GradeRecord> GradeRecords => Set<GradeRecord>();

    public DbSet<GradeExercisePoint> GradeExercisePoints => Set<GradeExercisePoint>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<AssignmentAcceptance> AssignmentAcceptances => Set<AssignmentAcceptance>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.DisplayName).HasMaxLength(256);
            e.Property(u => u.NeptunCode).HasMaxLength(32);
            e.Property(u => u.Affiliation).HasMaxLength(256);
            e.Property(u => u.GitHubUsername).HasMaxLength(128);

            // Filtered unique index: a Neptun code identifies a person, so no two accounts may share one.
            // NULL means "no code" (directory/local accounts may have none) and is allowed many times —
            // which is why the admin controllers store null, never "", for a blank code.
            e.HasIndex(u => u.NeptunCode).IsUnique().HasFilter("[NeptunCode] IS NOT NULL");
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

        builder.Entity<GitHubWebhookDelivery>(e =>
        {
            e.Property(d => d.DeliveryId).HasMaxLength(128);
            e.Property(d => d.EventName).HasMaxLength(64).IsRequired();
            e.Property(d => d.RepositoryFullName).HasMaxLength(400).IsRequired();
            e.Property(d => d.Error).HasMaxLength(2000);

            // The worker's claim query: oldest Pending row whose NextAttemptAt has come.
            e.HasIndex(d => new { d.Status, d.NextAttemptAt, d.Id });

            // The admin listing, and the retention pass.
            e.HasIndex(d => new { d.CourseId, d.ReceivedAt });
            e.HasIndex(d => d.ReceivedAt);

            // Not unique: an absent header stores null, and a GitHub redelivery is a legitimately new row.
            e.HasIndex(d => d.DeliveryId);

            // Cascade, and the only path from Course — which is why CoursesAdminController.Delete needs no
            // explicit ExecuteDeleteAsync for this table.
            e.HasOne(d => d.Course).WithMany().HasForeignKey(d => d.CourseId).OnDelete(DeleteBehavior.Cascade);

            // No query filter: see the remarks on the entity. The worker and the admin controller both read
            // this table with no current course, and a filter would make it silently appear empty.
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

        builder.Entity<Assignment>(e =>
        {
            e.Property(a => a.Name).HasMaxLength(256).IsRequired();
            e.Property(a => a.Description).HasMaxLength(1024);
            e.Property(a => a.TemplateRepoName).HasMaxLength(400).IsRequired();
            e.Property(a => a.InviteToken).HasMaxLength(128).IsRequired();

            // Globally unique: the invite link carries the token as its only identifier of the assignment, and
            // it is the capability that lets a stranger provision a repository — collisions are not an option.
            e.HasIndex(a => a.InviteToken).IsUnique();

            // The instructor listing reads "this course's open assignments".
            e.HasIndex(a => new { a.CourseId, a.ArchivedAt });

            e.HasOne(a => a.Course).WithMany().HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(a => a.CourseId == this.currentCourse.CurrentCourseId);
        });

        builder.Entity<AssignmentAcceptance>(e =>
        {
            e.Property(a => a.GitHubRepoName).HasMaxLength(400).IsRequired();
            e.Property(a => a.RepoUrl).HasMaxLength(1024).IsRequired();
            e.Property(a => a.GitHubUsername).HasMaxLength(128).IsRequired();

            // One repository per student per assignment. This index is the concurrency guard: a double click or
            // a second tab loses the race here rather than creating a second repository on GitHub.
            e.HasIndex(a => new { a.AssignmentId, a.UserId }).IsUnique();
            e.HasIndex(a => new { a.CourseId, a.GitHubRepoName });

            // The student home page reads every acceptance of one user across all courses.
            e.HasIndex(a => a.UserId);

            // NoAction on Course: Course cascades to Assignment which cascades to here, and SQL Server rejects
            // the second path. CoursesAdminController.Delete removes these rows explicitly because of it.
            e.HasOne(a => a.Course).WithMany().HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(a => a.Assignment).WithMany(x => x!.Acceptances).HasForeignKey(a => a.AssignmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(a => a.CourseId == this.currentCourse.CurrentCourseId);
        });
    }
}
