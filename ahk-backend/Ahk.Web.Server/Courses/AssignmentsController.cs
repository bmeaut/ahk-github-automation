using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Courses.Dto;
using Ahk.Web.Server.CourseContext;
using Ahk.Web.Services.Assignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ahk.Web.Server.Courses;

/// <summary>
/// Course-scoped assignment administration: what used to be set up in GitHub Classroom. Staff define an
/// assignment against a template repository and hand out its invite link; students provision themselves through
/// <see cref="AssignmentInviteController"/>.
///
/// Assignments are additive. Repositories created outside the portal keep working exactly as before, so nothing
/// here is a precondition for grading or status tracking.
/// </summary>
[ApiController]
[Route("api/{course}/assignments")]
[Authorize(Policy = CourseMembershipRequirement.PolicyName)]
public sealed class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService assignments;

    public AssignmentsController(IAssignmentService assignments) => this.assignments = assignments;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> List([FromQuery] bool includeArchived, CancellationToken cancellationToken)
    {
        var course = CurrentCourse();
        var items = await assignments.ListAsync(course.Id, includeArchived, cancellationToken);
        var counts = await assignments.CountAcceptancesAsync(course.Id, cancellationToken);

        return Ok(items.Select(a => ToDto(a, course, counts.GetValueOrDefault(a.Id))).ToList());
    }

    /// <summary>
    /// One assignment. <paramref name="checkTemplate"/> additionally asks GitHub whether the template
    /// repository exists and is marked as a template — a network call, so the editor opts into it rather than
    /// every listing paying for it.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDetailDto>> Get(int id, [FromQuery] bool checkTemplate, CancellationToken cancellationToken)
    {
        var course = CurrentCourse();
        var assignment = await assignments.GetAsync(course.Id, id, cancellationToken);
        if (assignment is null)
            return NotFound();

        var acceptances = await assignments.ListAcceptancesAsync(course.Id, id, cancellationToken);

        return Ok(new AssignmentDetailDto
        {
            Assignment = ToDto(assignment, course, acceptances.Count),
            Template = checkTemplate
                ? TemplateCheckDto.From(await assignments.CheckTemplateAsync(course.Id, assignment.TemplateRepoName, cancellationToken))
                : null,
        });
    }

    /// <summary>
    /// Advisory template check for a repository name the editor is still typing, before the assignment is saved.
    /// Same GitHub lookup as <see cref="Get"/>'s <c>checkTemplate</c>, but keyed on a name rather than a stored
    /// assignment — so the check is available while creating, not only while editing. Never blocks anything.
    /// </summary>
    [HttpPost("check-template")]
    [ProducesResponseType(typeof(TemplateCheckDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TemplateCheckDto>> CheckTemplate([FromBody] CheckTemplateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var course = CurrentCourse();
        var check = await assignments.CheckTemplateAsync(course.Id, request.TemplateRepoName ?? string.Empty, cancellationToken);

        return Ok(TemplateCheckDto.From(check));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentDto>> Create([FromBody] SaveAssignmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (Validate(request) is { } error)
            return BadRequest(new { error });

        var course = CurrentCourse();
        var assignment = await assignments.CreateAsync(course.Id, ToInput(request), cancellationToken);

        return CreatedAtAction(nameof(Get), new { course = course.Slug, id = assignment.Id }, ToDto(assignment, course, 0));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDto>> Update(int id, [FromBody] SaveAssignmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (Validate(request) is { } error)
            return BadRequest(new { error });

        var course = CurrentCourse();
        var assignment = await assignments.UpdateAsync(course.Id, id, ToInput(request), cancellationToken);

        return assignment is null ? NotFound() : Ok(ToDto(assignment, course, 0));
    }

    [HttpPost("{id:int}/archive")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<AssignmentDto>> Archive(int id, CancellationToken cancellationToken) =>
        SetArchivedAsync(id, archived: true, cancellationToken);

    [HttpPost("{id:int}/unarchive")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<AssignmentDto>> Unarchive(int id, CancellationToken cancellationToken) =>
        SetArchivedAsync(id, archived: false, cancellationToken);

    /// <summary>Issues a new invite link. Every copy of the previous one stops working immediately.</summary>
    [HttpPost("{id:int}/regenerate-invite")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDto>> RegenerateInvite(int id, CancellationToken cancellationToken)
    {
        var course = CurrentCourse();
        var assignment = await assignments.RegenerateInviteTokenAsync(course.Id, id, cancellationToken);

        return assignment is null ? NotFound() : Ok(ToDto(assignment, course, 0));
    }

    /// <summary>
    /// Deletes an assignment nobody has accepted. Once students hold repositories the record is the only trace
    /// of who got what, so the API refuses and points at archiving instead.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var course = CurrentCourse();

        var assignment = await assignments.GetAsync(course.Id, id, cancellationToken);
        if (assignment is null)
            return NotFound();

        if (!await assignments.DeleteAsync(course.Id, id, cancellationToken))
        {
            return Conflict(new
            {
                error = "Students have already accepted this assignment, so it cannot be deleted. Archive it instead — that closes the invite link and keeps their repositories linked.",
            });
        }

        return NoContent();
    }

    [HttpGet("{id:int}/acceptances")]
    [ProducesResponseType(typeof(IEnumerable<AssignmentAcceptanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AssignmentAcceptanceDto>>> ListAcceptances(int id, CancellationToken cancellationToken)
    {
        var course = CurrentCourse();
        var acceptances = await assignments.ListAcceptancesAsync(course.Id, id, cancellationToken);

        return Ok(acceptances.Select(a => new AssignmentAcceptanceDto
        {
            Id = a.Id,
            UserName = a.User?.UserName ?? string.Empty,
            DisplayName = a.User?.DisplayName,
            NeptunCode = a.User?.NeptunCode,
            GitHubUsername = a.GitHubUsername,
            GitHubRepoName = a.GitHubRepoName,
            RepoUrl = a.RepoUrl,
            AcceptedAt = a.AcceptedAt,
            InvitationPending = a.InvitationPending,
        }).ToList());
    }

    private async Task<ActionResult<AssignmentDto>> SetArchivedAsync(int id, bool archived, CancellationToken cancellationToken)
    {
        var course = CurrentCourse();
        var assignment = await assignments.SetArchivedAsync(course.Id, id, archived, cancellationToken);

        return assignment is null ? NotFound() : Ok(ToDto(assignment, course, 0));
    }

    private static string? Validate(SaveAssignmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "An assignment needs a name.";

        return string.IsNullOrWhiteSpace(request.TemplateRepoName)
            ? "An assignment needs a template repository."
            : null;
    }

    private static AssignmentInput ToInput(SaveAssignmentRequest request) => new()
    {
        Name = request.Name,
        Description = request.Description,
        TemplateRepoName = request.TemplateRepoName,
    };

    private AssignmentDto ToDto(Assignment assignment, Course course, int acceptanceCount) => new()
    {
        Id = assignment.Id,
        Name = assignment.Name,
        Description = assignment.Description,
        TemplateRepoName = assignment.TemplateRepoName,
        InvitePath = $"/{course.Slug}/invite/{assignment.InviteToken}",
        IsArchived = assignment.ArchivedAt is not null,
        ArchivedAt = assignment.ArchivedAt,
        CreatedAt = assignment.CreatedAt,
        AcceptanceCount = acceptanceCount,
    };

    private Course CurrentCourse() => (Course)HttpContext.Items[CourseResolutionMiddleware.CourseItemKey]!;
}
