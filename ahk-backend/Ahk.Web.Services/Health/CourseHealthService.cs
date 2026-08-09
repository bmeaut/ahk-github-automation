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
/// </summary>
public sealed class CourseHealthService : ICourseHealthService
{
    private readonly ApplicationDbContext db;
    private readonly IReadOnlyList<ICourseHealthCheck> checks;

    public CourseHealthService(ApplicationDbContext db, IEnumerable<ICourseHealthCheck> checks)
    {
        this.db = db;
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

        return new CourseHealthReport
        {
            CourseId = course.Id,
            CourseSlug = course.Slug,
            CourseName = course.Name,
            Checks = results,
        };
    }
}
