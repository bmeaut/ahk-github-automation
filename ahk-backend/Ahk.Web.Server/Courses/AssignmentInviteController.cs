using Ahk.Web.Data.Entities;
using Ahk.Web.Server.CourseContext;
using Ahk.Web.Services.Assignments;
using Ahk.Web.Services.GitHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ahk.Web.Server.Courses;

/// <summary>
/// The student side of an assignment: follow the invite link, confirm, get a repository. Replaces GitHub
/// Classroom's accept flow.
///
/// ⚠️ <c>[Authorize]</c> without the <c>CourseMember</c> policy, deliberately. Students are not members of the
/// course — accepting is how they first appear in it at all — so requiring membership here would lock every
/// student out of the one endpoint meant for them. <see cref="CourseResolutionMiddleware"/> still resolves the
/// {course} segment, so the course query filter behaves normally; the invite token is the capability that
/// authorizes the request.
/// </summary>
[ApiController]
[Route("api/{course}/invite/{token}")]
[Authorize]
public sealed class AssignmentInviteController : ControllerBase
{
    private readonly IAssignmentInviteService invites;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ILogger<AssignmentInviteController> logger;

    public AssignmentInviteController(
        IAssignmentInviteService invites,
        UserManager<ApplicationUser> userManager,
        ILogger<AssignmentInviteController> logger)
    {
        this.invites = invites;
        this.userManager = userManager;
        this.logger = logger;
    }

    /// <summary>Where this student stands: what is still missing, or the repository they already have.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(InviteState), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<InviteState>> Get(string token, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var course = CurrentCourse();
        return Ok(await invites.GetStateAsync(course.Id, token, user, cancellationToken));
    }

    /// <summary>
    /// Creates the student's repository and grants them access. Idempotent: accepting twice returns the same
    /// repository rather than creating a second one.
    /// </summary>
    [HttpPost("accept")]
    [ProducesResponseType(typeof(InviteState), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<InviteState>> Accept(string token, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var course = CurrentCourse();

        try
        {
            return Ok(await invites.AcceptAsync(course.Id, token, user, cancellationToken));
        }
        catch (GitHubOperationException ex)
        {
            // GitHub refused something we asked for. Its own words are far more useful to the instructor who
            // will be asked about it than "an error occurred".
            logger.LogError(ex, "Accepting invite {Token} in course {Course} failed at GitHub.", token, course.Slug);

            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = $"GitHub refused to set up the repository: {ex.GitHubMessage ?? ex.Message} Tell your instructor — this is a configuration problem, not something you did.",
            });
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Accepting invite {Token} in course {Course} could not reach GitHub.", token, course.Slug);

            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "GitHub could not be reached. Try again in a few minutes.",
            });
        }
    }

    private Course CurrentCourse() => (Course)HttpContext.Items[CourseResolutionMiddleware.CourseItemKey]!;
}
