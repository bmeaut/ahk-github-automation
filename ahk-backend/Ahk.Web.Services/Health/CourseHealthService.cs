using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Services.Health;

public interface ICourseHealthService
{
    /// <summary>Runs every registered check against one course. Returns null when the course does not exist.</summary>
    Task<CourseHealthReport?> CheckCourseAsync(int courseId, CancellationToken cancellationToken = default);

    /// <summary>Runs every registered check against every course.</summary>
    Task<IReadOnlyList<CourseHealthReport>> CheckAllCoursesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Runs the registered <see cref="ICourseHealthCheck"/>s and assembles the reports the admin dashboard shows.
///
/// Courses are checked sequentially: the checks share the request's <see cref="ApplicationDbContext"/>, which is
/// not thread-safe, and the only slow check (<see cref="GitHubAccessHealthCheck"/>) is bounded by its own
/// 10-second HTTP timeout.
///
/// <para>Every run also writes its verdict back onto the <see cref="Course"/> row. That is what lets the course
/// register show an integration state without running anything, and it means the cache is refreshed by all
/// three entry points — the dashboard, the course editor's re-check button, and the background worker —
/// without any of them knowing about it.</para>
/// </summary>
public sealed class CourseHealthService : ICourseHealthService
{
    /// <summary>Safety net matching the column length; five check titles fit comfortably inside it.</summary>
    private const int SummaryMaxLength = 400;

    private readonly ApplicationDbContext db;
    private readonly TimeProvider timeProvider;
    private readonly IReadOnlyList<ICourseHealthCheck> checks;

    public CourseHealthService(ApplicationDbContext db, TimeProvider timeProvider, IEnumerable<ICourseHealthCheck> checks)
    {
        this.db = db;
        this.timeProvider = timeProvider;
        this.checks = checks.OrderBy(c => c.Order).ThenBy(c => c.Id, StringComparer.Ordinal).ToList();
    }

    public async Task<CourseHealthReport?> CheckCourseAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var course = await LoadCourses().FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
        return course is null ? null : await RunChecksAsync(course, cancellationToken);
    }

    public async Task<IReadOnlyList<CourseHealthReport>> CheckAllCoursesAsync(CancellationToken cancellationToken = default)
    {
        var courses = await LoadCourses().OrderBy(c => c.Name).ThenBy(c => c.Slug).ToListAsync(cancellationToken);

        var reports = new List<CourseHealthReport>(courses.Count);
        foreach (var course in courses)
            reports.Add(await RunChecksAsync(course, cancellationToken));

        return reports;
    }

    private IQueryable<Course> LoadCourses() => db.Courses.AsNoTracking().Include(c => c.GitHubConfig);

    private async Task<CourseHealthReport> RunChecksAsync(Course course, CancellationToken cancellationToken)
    {
        var results = new List<HealthCheckResult>(checks.Count);

        foreach (var check in checks)
        {
            var started = DateTimeOffset.UtcNow;
            HealthCheckResult result;
            try
            {
                result = await check.RunAsync(course, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A check is not allowed to take the dashboard down with it.
                result = HealthCheckResult.Failed(check, $"The check could not complete: {ex.Message}");
            }

            var elapsed = (int)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
            results.Add(new HealthCheckResult
            {
                CheckId = result.CheckId,
                Title = result.Title,
                Status = result.Status,
                Message = result.Message,
                Remediation = result.Remediation,
                DurationMs = elapsed,
            });
        }

        var report = new CourseHealthReport
        {
            CourseId = course.Id,
            CourseSlug = course.Slug,
            CourseName = course.Name,
            CheckedAt = timeProvider.GetUtcNow(),
            Checks = results,
        };

        await CacheAsync(report, cancellationToken);
        return report;
    }

    /// <summary>
    /// Stores the aggregate verdict on the course row. The courses were read <c>AsNoTracking</c>, so this is a
    /// second, tracked read — not <c>ExecuteUpdateAsync</c>, which is relational-only and would break the
    /// InMemory tests.
    /// </summary>
    private async Task CacheAsync(CourseHealthReport report, CancellationToken cancellationToken)
    {
        var entity = await db.Courses.FirstOrDefaultAsync(c => c.Id == report.CourseId, cancellationToken);
        if (entity is null)
            return;

        var failing = string.Join(", ", report.Checks.Where(c => c.Status != HealthStatus.Healthy).Select(c => c.Title));

        entity.HealthStatus = report.Status;
        entity.HealthCheckedAt = report.CheckedAt;
        entity.HealthSummary = failing.Length > SummaryMaxLength ? failing[..SummaryMaxLength] : failing;

        await db.SaveChangesAsync(cancellationToken);
    }
}
