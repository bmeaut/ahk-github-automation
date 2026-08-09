using System.ComponentModel.DataAnnotations;
using Ahk.Web.Data.Entities;

namespace Ahk.Web.Server.Admin.Dto;

/// <summary>A registered user with the two things an admin edits: site roles and course assignments.</summary>
public sealed class UserDto
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    /// <summary>From the IdP's <c>neptun_code</c> claim; empty for local accounts.</summary>
    public string? NeptunCode { get; set; }

    /// <summary>From the IdP's <c>eduperson_scoped_affiliation</c> claim, values joined with ';'.</summary>
    public string? Affiliation { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public IReadOnlyList<UserCourseDto> Courses { get; set; } = Array.Empty<UserCourseDto>();

    /// <summary>True when the account signs in through the identity provider rather than a local password.</summary>
    public bool IsExternal { get; set; }

    /// <summary>True while the account is locked out, so the list can show why a sign-in is failing.</summary>
    public bool IsLockedOut { get; set; }
}

public sealed class UserCourseDto
{
    public int CourseId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public CourseRole Role { get; set; }
}

/// <summary>One page of users, plus the total so the UI can say "showing 25 of 340".</summary>
public sealed class UserListResponse
{
    public IReadOnlyList<UserDto> Items { get; set; } = Array.Empty<UserDto>();

    public int Total { get; set; }
}

public sealed class CreateUserRequest
{
    [Required]
    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(256)]
    public string? DisplayName { get; set; }

    /// <summary>Optional Neptun code. Must be empty, or unique across users.</summary>
    [MaxLength(32)]
    public string? NeptunCode { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public sealed class UpdateUserRequest
{
    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(256)]
    public string? DisplayName { get; set; }

    [MaxLength(32)]
    public string? NeptunCode { get; set; }
}

/// <summary>The complete set of site roles the user should hold; anything not listed is removed.</summary>
public sealed class UpdateUserRolesRequest
{
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

public sealed class UpsertUserCourseRequest
{
    [Required]
    public int CourseId { get; set; }

    public CourseRole Role { get; set; } = CourseRole.Instructor;
}

public sealed class SetPasswordRequest
{
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
