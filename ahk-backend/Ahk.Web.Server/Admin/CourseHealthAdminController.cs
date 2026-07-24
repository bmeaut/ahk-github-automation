using Ahk.Web.Data;
using Ahk.Web.Services.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ahk.Web.Server.Admin;

/// <summary>
/// Runs the course health checks on demand. Results are computed per request and never cached: an admin opens
/// this page precisely because they want to know the state right now, usually after changing a credential.
///
/// Checks are discovered through DI (<see cref="ICourseHealthCheck"/>), so extending what "healthy" means
/// requires no change here.
/// </summary>
[ApiController]
[Route("api/admin/health")]
[Authorize(Roles = Roles.Admin)]
public sealed class CourseHealthAdminController : ControllerBase
{
    private readonly ICourseHealthService health;

    public CourseHealthAdminController(ICourseHealthService health) => this.health = health;

    /// <summary>Health of every course — the admin health dashboard.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CourseHealthReport>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CourseHealthReport>>> CheckAll(CancellationToken cancellationToken) =>
        Ok(await health.CheckAllCoursesAsync(cancellationToken));

    /// <summary>Health of one course — used by the re-check button on the course editor.</summary>
    [HttpGet("{courseId:int}")]
    [ProducesResponseType(typeof(CourseHealthReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseHealthReport>> CheckCourse(int courseId, CancellationToken cancellationToken)
    {
        var report = await health.CheckCourseAsync(courseId, cancellationToken);
        return report is null ? NotFound() : Ok(report);
    }
}
