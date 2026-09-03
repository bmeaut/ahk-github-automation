using Ahk.Web.Data.Entities;
using Ahk.Web.Server.CourseContext;
using Ahk.Web.Services.Submissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ahk.Web.Server.Courses;

/// <summary>
/// Actions on a single submission. Only archiving today: putting a repository aside without deleting anything,
/// so a finished assignment's work stops crowding the list while its events and grades stay exactly as they
/// were.
///
/// <para>Restricted to the course's own admins (<see cref="CurrentCourseAdminRequirement"/>), unlike the
/// read endpoints, which any member may call. ⚠️ Not <c>CourseAdmin</c>: that policy reads the <c>{id}</c>
/// route value as a course id, and here <c>{id}</c> is a submission.</para>
/// </summary>
[ApiController]
[Route("api/{course}/submissions")]
[Authorize(Policy = CurrentCourseAdminRequirement.PolicyName)]
public sealed class SubmissionsController : ControllerBase
{
    private readonly ISubmissionArchiveService archive;

    public SubmissionsController(ISubmissionArchiveService archive) => this.archive = archive;

    /// <summary>Archives one submission. Archiving an assignment does this for its whole roster at once.</summary>
    [HttpPost("{id:int}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Archive(int id, CancellationToken cancellationToken) =>
        SetArchivedAsync(id, archived: true, cancellationToken);

    [HttpPost("{id:int}/unarchive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Unarchive(int id, CancellationToken cancellationToken) =>
        SetArchivedAsync(id, archived: false, cancellationToken);

    private async Task<IActionResult> SetArchivedAsync(int id, bool archived, CancellationToken cancellationToken)
    {
        var course = (Course)HttpContext.Items[CourseResolutionMiddleware.CourseItemKey]!;

        // Scoped to the resolved course, so a submission id from another course is a 404 rather than an edit.
        return await archive.SetArchivedAsync(course.Id, id, archived, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
