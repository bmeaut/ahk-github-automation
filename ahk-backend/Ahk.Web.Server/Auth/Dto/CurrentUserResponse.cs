namespace Ahk.Web.Server.Auth.Dto;

/// <summary>Shape returned by <c>GET /api/auth/me</c> — hydrates the SPA session with identity + course access.</summary>
public sealed class CurrentUserResponse
{
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public IReadOnlyList<CourseMembershipDto> Courses { get; set; } = Array.Empty<CourseMembershipDto>();
}

public sealed class CourseMembershipDto
{
    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
