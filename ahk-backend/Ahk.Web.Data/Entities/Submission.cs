namespace Ahk.Web.Data.Entities;

/// <summary>
/// One student's GitHub repository within a course — the anchor that status events and grades hang off.
/// Replaces the raw repository-name string that the original system used as its grouping key
/// (<c>StudentResult.GitHubRepoName</c> / <c>StatusEventBase.Repository</c>).
/// </summary>
public class Submission : ICourseScoped
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    /// <summary>Null while the student is unknown — the repository often exists before neptun.txt is pushed.</summary>
    public int? StudentId { get; set; }

    public Student? Student { get; set; }

    /// <summary>Full "owner/name", normalized with <see cref="Normalize.RepoName"/> (lowercase, trimmed).</summary>
    public string GitHubRepoName { get; set; } = string.Empty;

    /// <summary>GitHub's numeric repository id, when known.</summary>
    public long? GitHubRepoId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastEventAt { get; set; }

    public ICollection<SubmissionEvent> Events { get; } = new List<SubmissionEvent>();

    public ICollection<GradeRecord> Grades { get; } = new List<GradeRecord>();
}
