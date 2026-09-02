using System.Security.Claims;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Admin.Dto;
using Ahk.Web.Server.CourseContext;
using Ahk.Web.Services.Courses;
using Ahk.Web.Services.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ahk.Web.Server.Admin;

/// <summary>
/// Host/admin context (no {course} segment): the set of courses, their connected GitHub environments, their
/// staff and their CI callback tokens. This centralizes what used to be a separate per-course Azure deployment
/// plus its <c>AHK_*</c> application settings.
///
/// <para>Authorization is per action, not on the class. Most of this is site-admin only, but one course's
/// detail, its staff and its member picker are open to a <see cref="CourseRole.Admin"/> of that course as well,
/// through the <c>CourseAdmin</c> policy — and the two cannot be combined on one action, because multiple
/// <c>[Authorize]</c> attributes are ANDed. A new action therefore has to state its own attribute;
/// <c>CourseAdminAccessTests</c> fails the build if one does not.</para>
///
/// Reads of course-scoped entities (students, submissions, grades, tokens) use <c>IgnoreQueryFilters()</c> and
/// filter on <c>CourseId</c> themselves: there is no {course} segment here, so no current course is set and the
/// course filter would otherwise match nothing.
/// </summary>
[ApiController]
[Route("api/admin/courses")]
[Authorize]
public sealed class CoursesAdminController : ControllerBase
{
    private const int MinCandidateSearchLength = 2;
    private const int MaxCandidates = 10;

    private readonly ApplicationDbContext db;
    private readonly IWebhookTokenService webhookTokens;
    private readonly TimeProvider timeProvider;
    private readonly CourseHealthOptions healthOptions;

    public CoursesAdminController(
        ApplicationDbContext db,
        IWebhookTokenService webhookTokens,
        TimeProvider timeProvider,
        IOptions<CourseHealthOptions> healthOptions)
    {
        this.db = db;
        this.webhookTokens = webhookTokens;
        this.timeProvider = timeProvider;
        this.healthOptions = healthOptions?.Value ?? new CourseHealthOptions();
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IEnumerable<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CourseDto>>> List(CancellationToken cancellationToken)
    {
        // Computed before the query so the staleness comparison translates to SQL instead of pulling rows back.
        var cutoff = timeProvider.GetUtcNow() - healthOptions.CacheTtl;

        var courses = await db.Courses
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Slug)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Slug = c.Slug,
                Name = c.Name,
                GitHubOrganization = c.GitHubOrganization,
                RepoNamePrefix = c.RepoNamePrefix,
                CreatedAt = c.CreatedAt,
                IntegrationEnabled = c.GitHubConfig != null && c.GitHubConfig.Enabled,
                MemberCount = c.Memberships.Count,
                StudentCount = db.Students.IgnoreQueryFilters().Count(s => s.CourseId == c.Id),
                SubmissionCount = db.Submissions.IgnoreQueryFilters().Count(s => s.CourseId == c.Id),
                HealthStatus = c.HealthStatus,
                HealthCheckedAt = c.HealthCheckedAt,
                HealthSummary = c.HealthSummary,
                HealthStale = c.HealthCheckedAt == null || c.HealthCheckedAt < cutoff,
            })
            .ToListAsync(cancellationToken);

        return Ok(courses);
    }

    /// <summary>
    /// One course, as the course-management screen reads it. A course admin gets the same payload minus the
    /// GitHub integration and the callback tokens — see <see cref="CourseDetailDto"/>.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = CourseAdminRequirement.PolicyName)]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDetailDto>> Get(int id, CancellationToken cancellationToken)
    {
        var course = await db.Courses
            .AsNoTracking()
            .Include(c => c.GitHubConfig)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (course is null)
            return NotFound();

        var members = await db.CourseMemberships
            .AsNoTracking()
            .Where(m => m.CourseId == id)
            .OrderBy(m => m.User!.UserName)
            .Select(m => new CourseMemberDto
            {
                UserId = m.UserId,
                UserName = m.User!.UserName ?? string.Empty,
                DisplayName = m.User.DisplayName,
                Email = m.User.Email,
                Role = m.Role,
            })
            .ToListAsync(cancellationToken);

        // Withheld, not merely unrendered: the token DTOs carry their HMAC secrets in clear, and the integration
        // state is a site-admin concern. A course admin's browser never receives either.
        var siteAdmin = User.IsInRole(Roles.Admin);
        var tokens = siteAdmin
            ? await webhookTokens.ListForCourseAsync(id, cancellationToken)
            : Array.Empty<CourseWebhookToken>();

        return Ok(new CourseDetailDto
        {
            Id = course.Id,
            Slug = course.Slug,
            Name = course.Name,
            GitHubOrganization = course.GitHubOrganization,
            RepoNamePrefix = course.RepoNamePrefix,
            CreatedAt = course.CreatedAt,
            StudentCount = await db.Students.IgnoreQueryFilters().CountAsync(s => s.CourseId == id, cancellationToken),
            SubmissionCount = await db.Submissions.IgnoreQueryFilters().CountAsync(s => s.CourseId == id, cancellationToken),
            GitHubConfig = siteAdmin ? ToDto(course.GitHubConfig) : null,
            Members = members,
            WebhookTokens = tokens.Select(ToDto).ToList(),
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseDetailDto>> Create([FromBody] CreateCourseRequest request, CancellationToken cancellationToken)
    {
        if (await db.Courses.AnyAsync(c => c.Slug == request.Slug, cancellationToken))
            return Conflict(new { error = $"A course with slug '{request.Slug}' already exists." });

        var course = new Course
        {
            Slug = request.Slug,
            Name = request.Name,
            GitHubOrganization = Trimmed(request.GitHubOrganization),
            RepoNamePrefix = Trimmed(request.RepoNamePrefix),

            // Created up front so the course editor always has an integration section to fill in.
            GitHubConfig = new CourseGitHubConfig(),
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = course.Id }, new CourseDetailDto
        {
            Id = course.Id,
            Slug = course.Slug,
            Name = course.Name,
            GitHubOrganization = course.GitHubOrganization,
            RepoNamePrefix = course.RepoNamePrefix,
            CreatedAt = course.CreatedAt,
            GitHubConfig = ToDto(course.GitHubConfig),
        });
    }

    /// <summary>Site-admin only: a course admin sees these settings, but read-only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (course is null)
            return NotFound();

        if (!string.Equals(course.Slug, request.Slug, StringComparison.Ordinal)
            && await db.Courses.AnyAsync(c => c.Slug == request.Slug && c.Id != id, cancellationToken))
        {
            return Conflict(new { error = $"A course with slug '{request.Slug}' already exists." });
        }

        course.Slug = request.Slug;
        course.Name = request.Name;
        course.GitHubOrganization = Trimmed(request.GitHubOrganization);
        course.RepoNamePrefix = Trimmed(request.RepoNamePrefix);

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes a course and, by cascade, everything assigned to it — students, submissions, events, grades,
    /// memberships and tokens. The caller must repeat the slug to confirm, so a mis-clicked id cannot erase a
    /// live course's grades.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, [FromQuery] string? confirmSlug, CancellationToken cancellationToken)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (course is null)
            return NotFound();

        if (!string.Equals(course.Slug, confirmSlug, StringComparison.Ordinal))
            return BadRequest(new { error = $"Type the course slug '{course.Slug}' to confirm deletion." });

        // GradeRecord, SubmissionEvent and AssignmentAcceptance point at Course with NoAction (SQL Server
        // rejects the extra cascade paths), so they are removed explicitly before the course goes.
        //
        // GitHubWebhookDeliveries is deliberately absent: it is reached by a single path from Course, so its
        // foreign key is a plain Cascade and the database removes it. Adding it here would be harmless but
        // would suggest the rule is "list everything", which is what makes people give a new entity a second
        // cascade path without noticing.
        await db.GradeExercisePoints.IgnoreQueryFilters()
            .Where(p => p.GradeRecord!.CourseId == id).ExecuteDeleteAsync(cancellationToken);
        await db.GradeRecords.IgnoreQueryFilters().Where(g => g.CourseId == id).ExecuteDeleteAsync(cancellationToken);
        await db.SubmissionEvents.IgnoreQueryFilters().Where(e => e.CourseId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Submissions.IgnoreQueryFilters().Where(s => s.CourseId == id).ExecuteDeleteAsync(cancellationToken);
        await db.AssignmentAcceptances.IgnoreQueryFilters().Where(a => a.CourseId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Assignments.IgnoreQueryFilters().Where(a => a.CourseId == id).ExecuteDeleteAsync(cancellationToken);

        db.Courses.Remove(course);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ---- GitHub integration ----

    [HttpGet("{id:int}/github")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(CourseGitHubConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseGitHubConfigDto>> GetGitHubConfig(int id, CancellationToken cancellationToken)
    {
        var course = await db.Courses.AsNoTracking().Include(c => c.GitHubConfig)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return course is null ? NotFound() : Ok(ToDto(course.GitHubConfig));
    }

    [HttpPut("{id:int}/github")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(CourseGitHubConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseGitHubConfigDto>> UpdateGitHubConfig(int id, [FromBody] UpdateCourseGitHubConfigRequest request, CancellationToken cancellationToken)
    {
        var course = await db.Courses.Include(c => c.GitHubConfig).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (course is null)
            return NotFound();

        var config = course.GitHubConfig;
        if (config is null)
        {
            config = new CourseGitHubConfig { CourseId = id };
            db.CourseGitHubConfigs.Add(config);
            course.GitHubConfig = config;
        }

        config.GitHubAppId = Trimmed(request.GitHubAppId);
        config.GitHubAppPrivateKey = ApplySecret(config.GitHubAppPrivateKey, request.GitHubAppPrivateKey);
        config.GitHubAccessToken = ApplySecret(config.GitHubAccessToken, request.GitHubAccessToken);
        config.GitHubWebhookSecret = ApplySecret(config.GitHubWebhookSecret, request.GitHubWebhookSecret);
        config.WorkflowRunThreshold = request.WorkflowRunThreshold;
        config.Enabled = request.Enabled;
        config.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(config));
    }

    // ---- Members ----

    [HttpGet("{id:int}/members")]
    [Authorize(Policy = CourseAdminRequirement.PolicyName)]
    [ProducesResponseType(typeof(IEnumerable<CourseMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CourseMemberDto>>> ListMembers(int id, CancellationToken cancellationToken)
    {
        var members = await db.CourseMemberships
            .AsNoTracking()
            .Where(m => m.CourseId == id)
            .OrderBy(m => m.User!.UserName)
            .Select(m => new CourseMemberDto
            {
                UserId = m.UserId,
                UserName = m.User!.UserName ?? string.Empty,
                DisplayName = m.User.DisplayName,
                Email = m.User.Email,
                Role = m.Role,
            })
            .ToListAsync(cancellationToken);

        return Ok(members);
    }

    /// <summary>Adds a user to the course, or changes the role they hold in it.</summary>
    [HttpPut("{id:int}/members")]
    [Authorize(Policy = CourseAdminRequirement.PolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpsertMember(int id, [FromBody] UpsertCourseMemberRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == id, cancellationToken))
            return NotFound();

        if (!await db.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken))
            return NotFound(new { error = "No such user." });

        if (IsSelf(request.UserId))
            return BadRequest(new { error = "You cannot change your own role in this course. Ask a site administrator." });

        var membership = await db.CourseMemberships
            .FirstOrDefaultAsync(m => m.CourseId == id && m.UserId == request.UserId, cancellationToken);

        if (membership is null)
            db.CourseMemberships.Add(new CourseMembership { CourseId = id, UserId = request.UserId, Role = request.Role });
        else
            membership.Role = request.Role;

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}/members/{userId:int}")]
    [Authorize(Policy = CourseAdminRequirement.PolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(int id, int userId, CancellationToken cancellationToken)
    {
        if (IsSelf(userId))
            return BadRequest(new { error = "You cannot remove yourself from this course. Ask a site administrator." });

        var membership = await db.CourseMemberships
            .FirstOrDefaultAsync(m => m.CourseId == id && m.UserId == userId, cancellationToken);

        if (membership is null)
            return NotFound();

        db.CourseMemberships.Remove(membership);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Users who could be added as staff, for the picker on the course-management screen. A course admin has no
    /// access to <c>api/admin/users</c>, but adding staff means finding people, so the search lives here and
    /// returns the minimum a picker needs.
    /// </summary>
    [HttpGet("{id:int}/member-candidates")]
    [Authorize(Policy = CourseAdminRequirement.PolicyName)]
    [ProducesResponseType(typeof(IEnumerable<CourseMemberCandidateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CourseMemberCandidateDto>>> MemberCandidates(
        int id,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        // Below the minimum this would return an arbitrary slice of the directory rather than a search result.
        var term = search?.Trim() ?? string.Empty;
        if (term.Length < MinCandidateSearchLength)
            return Ok(Array.Empty<CourseMemberCandidateDto>());

        var candidates = await db.Users
            .AsNoTracking()
            .Where(u =>
                (u.UserName != null && u.UserName.Contains(term)) ||
                (u.DisplayName != null && u.DisplayName.Contains(term)) ||
                (u.Email != null && u.Email.Contains(term)) ||
                (u.NeptunCode != null && u.NeptunCode.Contains(term)))
            .OrderBy(u => u.UserName)
            .Take(MaxCandidates)
            .Select(u => new CourseMemberCandidateDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                DisplayName = u.DisplayName,
                Email = u.Email,
            })
            .ToListAsync(cancellationToken);

        return Ok(candidates);
    }

    // ---- CI callback tokens ----

    [HttpGet("{id:int}/tokens")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IEnumerable<WebhookTokenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WebhookTokenDto>>> ListTokens(int id, CancellationToken cancellationToken)
    {
        var tokens = await webhookTokens.ListForCourseAsync(id, cancellationToken);
        return Ok(tokens.Select(ToDto).ToList());
    }

    /// <summary>Issues a token, returning its secret. The secret is also readable later via the list endpoint.</summary>
    [HttpPost("{id:int}/tokens")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(WebhookTokenDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WebhookTokenDto>> CreateToken(int id, [FromBody] CreateWebhookTokenRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == id, cancellationToken))
            return NotFound();

        var token = await webhookTokens.CreateAsync(id, Trimmed(request.Description), cancellationToken);
        return CreatedAtAction(nameof(ListTokens), new { id }, ToDto(token));
    }

    [HttpDelete("{id:int}/tokens/{tokenId:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeToken(int id, int tokenId, CancellationToken cancellationToken) =>
        await webhookTokens.RevokeAsync(id, tokenId, cancellationToken) ? NoContent() : NotFound();

    /// <summary>
    /// True when the caller is acting on their own membership and is not a site admin. A course admin who
    /// demoted or removed themselves would lock themselves out of the course they administer — the same
    /// self-lockout the users endpoints refuse for the site Admin role and for account deletion. A site admin
    /// is not restricted: they can always get back in.
    /// </summary>
    private bool IsSelf(int userId) =>
        !User.IsInRole(Roles.Admin)
        && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId)
        && callerId == userId;

    /// <summary>
    /// Applies the credential update rule: null leaves the stored value alone, empty clears it, anything else
    /// replaces it. The UI sends null for a field the admin did not type into, so an unchanged form never
    /// wipes a secret it was never shown.
    /// </summary>
    private static string? ApplySecret(string? current, string? incoming) => incoming switch
    {
        null => current,
        "" => null,
        _ => incoming.Trim(),
    };

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CourseGitHubConfigDto ToDto(CourseGitHubConfig? config) => new()
    {
        GitHubAppId = config?.GitHubAppId,
        HasAppPrivateKey = !string.IsNullOrEmpty(config?.GitHubAppPrivateKey),
        HasAccessToken = !string.IsNullOrEmpty(config?.GitHubAccessToken),
        AccessTokenHint = Hint(config?.GitHubAccessToken),
        HasWebhookSecret = !string.IsNullOrEmpty(config?.GitHubWebhookSecret),
        WorkflowRunThreshold = config?.WorkflowRunThreshold ?? 5,
        Enabled = config?.Enabled ?? false,
        UpdatedAt = config?.UpdatedAt,
    };

    /// <summary>Last four characters of a secret — enough to recognize which one is stored, not enough to use.</summary>
    private static string? Hint(string? secret) =>
        string.IsNullOrEmpty(secret) || secret.Length < 8 ? null : secret[^4..];

    // The CI callback secret is a plaintext column (the HMAC scheme needs the raw key), and every token endpoint
    // here is admin-only. An admin who can mint a token can already obtain a working secret, so returning an
    // existing one is no extra exposure — it lets the console copy a secret again rather than force a re-issue.
    private static WebhookTokenDto ToDto(CourseWebhookToken token) => new()
    {
        Id = token.Id,
        Token = token.Token,
        Secret = token.Secret,
        Description = token.Description,
        CreatedAt = token.CreatedAt,
        RevokedAt = token.RevokedAt,
    };
}
