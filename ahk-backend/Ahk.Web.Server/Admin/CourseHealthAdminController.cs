using Ahk.Web.Data;
using Ahk.Web.Server.CourseContext;
using Ahk.Web.Services.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ahk.Web.Server.Admin;

/// <summary>
/// Runs the course health checks on demand. The GET endpoints always run live: an admin opens the health
/// dashboard precisely because they want to know the state right now, usually after changing a credential.
/// Every run also refreshes the cached verdict on the course row as a side effect (see
/// <see cref="ICourseHealthService"/>), which is what the course register reads.
///
/// <para><see cref="RefreshStale"/> is the other direction: it queues work and returns immediately, so a screen
/// can ask for stale verdicts to be brought up to date without waiting for them.</para>
///
/// Checks are discovered through DI (<see cref="ICourseHealthCheck"/>), so extending what "healthy" means
/// requires no change here.
///
/// <para>Authorization is per action: the whole-site views stay site-admin only, while one course's report is
/// also open to that course's own admins through the <c>CourseAdmin</c> policy. A report carries no credential,
/// only verdicts, so it needs no redaction.</para>
/// </summary>
[ApiController]
[Route("api/admin/health")]
[Authorize]
public sealed class CourseHealthAdminController : ControllerBase
{
    private readonly ICourseHealthService health;
    private readonly ApplicationDbContext db;
    private readonly ICourseHealthRefreshQueue refreshQueue;
    private readonly TimeProvider timeProvider;
    private readonly CourseHealthOptions options;

    public CourseHealthAdminController(
        ICourseHealthService health,
        ApplicationDbContext db,
        ICourseHealthRefreshQueue refreshQueue,
        TimeProvider timeProvider,
        IOptions<CourseHealthOptions> options)
    {
        this.health = health;
        this.db = db;
        this.refreshQueue = refreshQueue;
        this.timeProvider = timeProvider;
        this.options = options?.Value ?? new CourseHealthOptions();
    }

    /// <summary>Health of every course — the admin health dashboard.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IEnumerable<CourseHealthReport>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CourseHealthReport>>> CheckAll(CancellationToken cancellationToken) =>
        Ok(await health.CheckAllCoursesAsync(cancellationToken));

    /// <summary>
    /// Health of one course — used by the re-check button on the course-management screen, which its own admins
    /// can open. The route parameter is named <c>id</c> because that is where the <c>CourseAdmin</c> policy
    /// reads the course from.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = CourseAdminRequirement.PolicyName)]
    [ProducesResponseType(typeof(CourseHealthReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseHealthReport>> CheckCourse(int id, CancellationToken cancellationToken)
    {
        var report = await health.CheckCourseAsync(id, cancellationToken);
        return report is null ? NotFound() : Ok(report);
    }

    /// <summary>
    /// Queues a background re-check of every course whose cached verdict is older than the TTL. Returns as soon
    /// as the ids are queued — no check runs on this request — so the course register can fire it and forget it.
    /// </summary>
    [HttpPost("refresh-stale")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RefreshStale(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow() - options.CacheTtl;

        var stale = await db.Courses
            .AsNoTracking()
            .Where(c => c.HealthCheckedAt == null || c.HealthCheckedAt < cutoff)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (var courseId in stale)
            refreshQueue.Enqueue(courseId);

        return Accepted(new { queued = stale.Count });
    }
}
