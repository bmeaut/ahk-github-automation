namespace Ahk.Web.Data.Entities;

/// <summary>
/// A university course (e.g. BME subject code "viaubc01"). This is what used to be a separate
/// per-course Azure Functions deployment; each course now lives as one record in the central site
/// and holds its own GitHub-environment configuration. Domain data is assigned to a course via
/// <see cref="ICourseScoped.CourseId"/>.
/// </summary>
public class Course
{
    public int Id { get; set; }

    /// <summary>URL-safe unique identifier used in the path segment: ahk.aut.bme.hu/{Slug}/...</summary>
    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // --- Repository routing ---
    // These two live on Course (not CourseGitHubConfig) because machine-to-machine entry points resolve
    // the course from them, and CourseResolutionMiddleware loads Course on every course-scoped request.
    // Credentials deliberately live in CourseGitHubConfig so they are not on that hot path.

    /// <summary>GitHub organization owning this course's repositories — the primary resolution key.</summary>
    public string? GitHubOrganization { get; set; }

    /// <summary>
    /// Optional repository-name prefix, used to disambiguate when one organization hosts several courses.
    /// This is the explicit form of what used to be the implicit "repo prefix = course" convention.
    /// </summary>
    public string? RepoNamePrefix { get; set; }

    public CourseGitHubConfig? GitHubConfig { get; set; }

    public ICollection<CourseMembership> Memberships { get; } = new List<CourseMembership>();
}
