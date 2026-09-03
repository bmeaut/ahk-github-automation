using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Auth;
using Ahk.Web.Server.CourseContext;
using Ahk.Web.Services.StatusTracking;
using Ahk.Web.Services.StatusTracking.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ahk.Web.Server.Courses;

/// <summary>
/// Course-scoped submission status list — the port of the legacy <c>list-statuses/{*repoprefix}</c> function,
/// with the repository prefix replaced by the {course} route segment.
///
/// <para>One of the two endpoints that also accept a personal access token — see <see cref="AuthSchemes"/>.</para>
///
/// <para>Archived submissions are left out unless <c>?includeArchived=true</c>, mirroring the assignments
/// listing, so a script gets the course's live picture without knowing archiving exists.</para>
/// </summary>
[ApiController]
[Route("api/{course}/statuses")]
[Authorize(AuthenticationSchemes = AuthSchemes.CookieOrPersonalToken, Policy = CourseMembershipRequirement.PolicyName)]
public sealed class SubmissionStatusesController : ControllerBase
{
    private readonly IStatusTrackingService statusTracking;

    public SubmissionStatusesController(IStatusTrackingService statusTracking) => this.statusTracking = statusTracking;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RepositoryStatus>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RepositoryStatus>>> List(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        var course = (Course)HttpContext.Items[CourseResolutionMiddleware.CourseItemKey]!;
        return Ok(await statusTracking.ListStatusesAsync(course.Id, includeArchived, cancellationToken));
    }
}
