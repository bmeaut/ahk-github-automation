namespace Ahk.Web.Server.Auth.Dto;

/// <summary>Shape returned by <c>GET /api/auth/me</c> — hydrates the SPA session with identity + course access.</summary>
public sealed class CurrentUserResponse
{
    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    /// <summary>
    /// From the IdP's <c>neptun_code</c> claim. The invite flow needs it to name a student's repository, and
    /// the SPA needs to know whether it is there before offering to accept an assignment.
    /// </summary>
    public string? NeptunCode { get; set; }

    /// <summary>Verified GitHub login, or null while the user has not supplied one.</summary>
    public string? GitHubUsername { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Every course this user can open, which is what the course switcher lists. For a site admin that is all
    /// courses, matching <c>CourseMembershipAuthorizationHandler</c>, which lets admins into any course.
    /// </summary>
    public IReadOnlyList<CourseMembershipDto> Courses { get; set; } = Array.Empty<CourseMembershipDto>();
}

public sealed class CourseMembershipDto
{
    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// True when access comes from the site-admin role rather than a membership record. The UI marks these so
    /// an admin can see they are working inside a course they were not explicitly assigned to.
    /// </summary>
    public bool ViaSiteAdmin { get; set; }
}
