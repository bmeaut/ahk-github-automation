using Ahk.Web.Data;
using Ahk.Web.Services.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ahk.Web.Server.Admin;

/// <summary>
/// Refreshes stale course health verdicts off the request thread. The course register reads only the cached
/// verdict on the course row, so nothing on screen waits for the seconds of GitHub round-trips a real run
/// costs; opening the register simply queues whatever has gone stale, and this worker catches up.
///
/// <para>One course at a time, matching <see cref="ICourseHealthService"/>'s own sequential design and the
/// single-worker decision behind the webhook queue. There is no periodic sweep: a verdict nobody looks at does
/// not need refreshing.</para>
/// </summary>
public sealed class CourseHealthRefreshWorker : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ICourseHealthRefreshQueue queue;
    private readonly TimeProvider timeProvider;
    private readonly CourseHealthOptions options;
    private readonly ILogger<CourseHealthRefreshWorker> logger;

    public CourseHealthRefreshWorker(
        IServiceScopeFactory scopeFactory,
        ICourseHealthRefreshQueue queue,
        TimeProvider timeProvider,
        IOptions<CourseHealthOptions> options,
        ILogger<CourseHealthRefreshWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.queue = queue;
        this.timeProvider = timeProvider;
        this.options = options?.Value ?? new CourseHealthOptions();
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Course health refresh worker started, cache TTL {CacheTtl}", options.CacheTtl);

        try
        {
            await foreach (var courseId in queue.DequeueAllAsync(stoppingToken))
                await RefreshAsync(courseId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutting down.
        }

        logger.LogInformation("Course health refresh worker stopped");
    }

    private async Task RefreshAsync(int courseId, CancellationToken stoppingToken)
    {
        try
        {
            // One scope per course: the health service and its checks are scoped over a request-shaped
            // DbContext, and a long-lived one would accumulate tracked entities forever.
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Re-read the timestamp rather than trusting the queued request. This is the de-duplication:
            // several admins opening the register, or one reloading it during a run, all collapse into the
            // single run that actually refreshed the row.
            var checkedAt = await db.Courses
                .AsNoTracking()
                .Where(c => c.Id == courseId)
                .Select(c => c.HealthCheckedAt)
                .FirstOrDefaultAsync(stoppingToken);

            if (checkedAt is not null && timeProvider.GetUtcNow() - checkedAt.Value < options.CacheTtl)
                return;

            var health = scope.ServiceProvider.GetRequiredService<ICourseHealthService>();
            var report = await health.CheckCourseAsync(courseId, stoppingToken);

            if (report is not null)
                logger.LogInformation("Refreshed health for course {CourseSlug}: {Status}", report.CourseSlug, report.Status);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
#pragma warning disable CA1031 // The loop must survive anything: a GitHub outage must not kill the worker.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "Course health refresh failed for course {CourseId}", courseId);
        }
    }
}
