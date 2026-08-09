using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Assignments;
using Ahk.Web.Services.GitHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ahk.Web.Server.Students;

/// <summary>
/// A student's own view: every repository they hold, across every course, and a way to re-send a GitHub
/// invitation that lapsed before they clicked it.
///
/// No {course} segment — a student's repositories span courses, and they are members of none of them. The
/// service behind this filters on the user id with <c>IgnoreQueryFilters()</c> for exactly that reason.
/// </summary>
[ApiController]
[Route("api/my/assignments")]
[Authorize]
public sealed class MyAssignmentsController : ControllerBase
{
    private readonly IStudentAssignmentService studentAssignments;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ILogger<MyAssignmentsController> logger;

    public MyAssignmentsController(
        IStudentAssignmentService studentAssignments,
        UserManager<ApplicationUser> userManager,
        ILogger<MyAssignmentsController> logger)
    {
        this.studentAssignments = studentAssignments;
        this.userManager = userManager;
        this.logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StudentRepository>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<StudentRepository>>> List(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        return Ok(await studentAssignments.ListForUserAsync(user.Id, cancellationToken));
    }

    /// <summary>
    /// Withdraws the stale invitation and issues a fresh one. GitHub has no way to extend an invitation, so
    /// replacing it is the only route back in for a student who missed the window.
    /// </summary>
    [HttpPost("{id:int}/resend-invitation")]
    [ProducesResponseType(typeof(StudentRepository), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<StudentRepository>> ResendInvitation(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        try
        {
            // Scoped to the caller's own acceptances, so another student's id is a 404, not someone else's repo.
            var result = await studentAssignments.ResendInvitationAsync(user.Id, id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (GitHubOperationException ex)
        {
            logger.LogError(ex, "Re-sending the invitation for acceptance {AcceptanceId} failed at GitHub.", id);

            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = $"GitHub refused to send the invitation: {ex.GitHubMessage ?? ex.Message}",
            });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "GitHub could not be reached. Try again in a few minutes." });
        }
    }
}
