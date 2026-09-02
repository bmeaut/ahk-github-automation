using System.ComponentModel.DataAnnotations;
using Ahk.Web.Data.Entities;

namespace Ahk.Web.Server.Admin.Dto;

/// <summary>A course as it appears in the admin list: identity plus enough counts to judge it at a glance.</summary>
public sealed class CourseDto
{
    public int Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? GitHubOrganization { get; set; }

    public string? RepoNamePrefix { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>False when the course's GitHub integration is switched off (or not created yet).</summary>
    public bool IntegrationEnabled { get; set; }

    public int MemberCount { get; set; }

    public int StudentCount { get; set; }

    public int SubmissionCount { get; set; }

    // --- Cached health verdict ---
    // Read straight off the course row, never computed here: a live run costs seconds of GitHub round-trips
    // per course, and the register must paint in one query. /admin/health is the live view.

    /// <summary>Worst check status as of <see cref="HealthCheckedAt"/>; null when the course was never checked.</summary>
    public HealthStatus? HealthStatus { get; set; }

    public DateTimeOffset? HealthCheckedAt { get; set; }

    /// <summary>Titles of the checks that did not pass, comma-joined. Empty when everything passed.</summary>
    public string? HealthSummary { get; set; }

    /// <summary>True when the cached verdict is past its TTL. It is still shown; a refresh is queued behind it.</summary>
    public bool HealthStale { get; set; }
}

/// <summary>Everything one course holds, for the course editor: settings, integration, members and tokens.</summary>
public sealed class CourseDetailDto
{
    public int Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? GitHubOrganization { get; set; }

    public string? RepoNamePrefix { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int StudentCount { get; set; }

    public int SubmissionCount { get; set; }

    /// <summary>
    /// Null for a course admin: the GitHub integration is a site-admin concern, and its block is not rendered
    /// for them. The state is withheld rather than merely hidden, so the browser never receives it.
    /// </summary>
    public CourseGitHubConfigDto? GitHubConfig { get; set; }

    public IReadOnlyList<CourseMemberDto> Members { get; set; } = Array.Empty<CourseMemberDto>();

    /// <summary>Empty for a course admin, for the same reason — and these carry the callback secrets in clear.</summary>
    public IReadOnlyList<WebhookTokenDto> WebhookTokens { get; set; } = Array.Empty<WebhookTokenDto>();
}

/// <summary>
/// A user offered by the staff picker. Deliberately thinner than <see cref="UserDto"/>: course admins may
/// search the directory to add staff, but site roles and other courses' assignments are none of their business.
/// </summary>
public sealed class CourseMemberCandidateDto
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Email { get; set; }
}

public sealed class CreateCourseRequest
{
    [Required]
    [RegularExpression("^[a-z0-9-]{2,64}$", ErrorMessage = "Slug must be 2-64 chars: lowercase letters, digits, hyphen.")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? GitHubOrganization { get; set; }

    [MaxLength(256)]
    public string? RepoNamePrefix { get; set; }
}

/// <summary>
/// Course settings. The slug is included because it is editable, but changing it changes every URL the course
/// is reachable at — the UI warns before saving.
/// </summary>
public sealed class UpdateCourseRequest
{
    [Required]
    [RegularExpression("^[a-z0-9-]{2,64}$", ErrorMessage = "Slug must be 2-64 chars: lowercase letters, digits, hyphen.")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? GitHubOrganization { get; set; }

    [MaxLength(256)]
    public string? RepoNamePrefix { get; set; }
}

/// <summary>
/// The course's GitHub integration as the admin UI sees it. Stored credentials are never sent back — only
/// whether one is present, and a last-four hint so an admin can tell which token is in place.
/// </summary>
public sealed class CourseGitHubConfigDto
{
    public string? GitHubAppId { get; set; }

    public bool HasAppPrivateKey { get; set; }

    public bool HasAccessToken { get; set; }

    /// <summary>Last four characters of the stored access token, e.g. "…f3Ab". Null when none is stored.</summary>
    public string? AccessTokenHint { get; set; }

    public bool HasWebhookSecret { get; set; }

    public int WorkflowRunThreshold { get; set; }

    public bool Enabled { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Update for the GitHub integration. The three credential fields follow one rule: <c>null</c> leaves the
/// stored value alone (the UI sends null when the admin did not touch the field), an empty string clears it,
/// and any other value replaces it.
/// </summary>
public sealed class UpdateCourseGitHubConfigRequest
{
    [MaxLength(64)]
    public string? GitHubAppId { get; set; }

    public string? GitHubAppPrivateKey { get; set; }

    [MaxLength(512)]
    public string? GitHubAccessToken { get; set; }

    [MaxLength(512)]
    public string? GitHubWebhookSecret { get; set; }

    [Range(1, 1000)]
    public int WorkflowRunThreshold { get; set; } = 5;

    public bool Enabled { get; set; } = true;
}

public sealed class CourseMemberDto
{
    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Email { get; set; }

    public CourseRole Role { get; set; }
}

public sealed class UpsertCourseMemberRequest
{
    [Required]
    public int UserId { get; set; }

    public CourseRole Role { get; set; } = CourseRole.Instructor;
}

/// <summary>
/// A CI callback token. <see cref="Secret"/> is populated only in the response that creates the token — it is
/// never readable afterwards, so the UI shows it once and tells the admin to copy it.
/// </summary>
public sealed class WebhookTokenDto
{
    public int Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public string? Secret { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class CreateWebhookTokenRequest
{
    [MaxLength(512)]
    public string? Description { get; set; }
}
