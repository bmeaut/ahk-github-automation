using System.ComponentModel.DataAnnotations;

namespace Ahk.Web.Server.Admin.Dto;

public sealed class CourseDto
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? GitHubOrganization { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CreateCourseRequest
{
    [Required]
    [RegularExpression("^[a-z0-9-]{2,64}$", ErrorMessage = "Slug must be 2-64 chars: lowercase letters, digits, hyphen.")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public string? GitHubOrganization { get; set; }
}
