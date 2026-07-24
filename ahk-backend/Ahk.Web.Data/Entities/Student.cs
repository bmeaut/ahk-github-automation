namespace Ahk.Web.Data.Entities;

/// <summary>
/// A student within a course, keyed by Neptun code (BME's student identifier). Replaces the neptun string
/// that was denormalized onto every grade and pull-request event in the original system.
/// Rows are created on first sighting (from <c>neptun.txt</c> or a pull-request payload); no roster import
/// is required.
/// </summary>
public class Student : ICourseScoped
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    /// <summary>Normalized with <see cref="Normalize.Neptun"/> (uppercase, trimmed).</summary>
    public string Neptun { get; set; } = string.Empty;

    public string? GitHubUsername { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Submission> Submissions { get; } = new List<Submission>();
}
