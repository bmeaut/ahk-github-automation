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

    /// <summary>The GitHub login the user gave, or null while they have not supplied one.</summary>
    public string? GitHubUsername { get; set; }

    /// <summary>
    /// False while that login is only the user's own claim — nothing has confirmed they hold it. It turns true
    /// once a repository invitation sent to it is accepted, so it is false for everyone until their first
    /// assignment. The SPA says so plainly rather than presenting an unconfirmed name as settled.
    /// </summary>
    public bool GitHubUsernameVerified { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// User name of the site admin currently impersonating this account, or null in an ordinary session. The
    /// admin's user id stays server-side (in the signed cookie); the SPA only needs a name for the banner and
    /// the fact that a way back exists.
    /// </summary>
    public string? ImpersonatorUserName { get; set; }

    /// <summary>
    /// Every course this user can open, which is what the course switcher lists. For a site admin that is all
    /// courses, matching <c>CourseMembershipAuthorizationHandler</c>, which lets admins into any course.
    /// </summary>
    public IReadOnlyList<CourseMembershipDto> Courses { get; set; } = Array.Empty<CourseMembershipDto>();
}

public sealed class CourseMembershipDto
{
    /// <summary>The course's id, which is how the course-management screen addresses it.</summary>
    public int Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// True when access comes from the site-admin role rather than a membership record. The UI marks these so
    /// an admin can see they are working inside a course they were not explicitly assigned to.
    /// </summary>
    public bool ViaSiteAdmin { get; set; }
}
