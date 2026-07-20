namespace Ahk.Web.Data.Entities;

/// <summary>
/// A university course (e.g. BME subject code "viaubc01"). This is what used to be a separate
/// per-course Azure Functions deployment; each course now lives as one record in the central site
/// and holds its own GitHub-environment configuration. Domain data is assigned to a course via
/// <see cref="ICourseScoped.CourseId"/>.
/// </summary>
public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>URL-safe unique identifier used in the path segment: ahk.aut.bme.hu/{Slug}/...</summary>
    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // --- Placeholders for the GitHub-environment config ported from the per-course Azure deployments. ---
    // Populated in the porting milestone (GitHub App id/private key, webhook secret, queue/connection
    // settings, etc.). Kept nullable so the skeleton runs before the port.
    public string? GitHubOrganization { get; set; }

    public ICollection<CourseMembership> Memberships { get; } = new List<CourseMembership>();
}
