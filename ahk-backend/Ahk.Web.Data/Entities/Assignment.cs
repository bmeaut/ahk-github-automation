namespace Ahk.Web.Data.Entities;

/// <summary>
/// A piece of homework a course hands out: a template repository plus the invite link students use to get their
/// own copy of it. This is the part of the lifecycle GitHub Classroom used to own.
///
/// Assignments are deliberately *additive*: a repository does not need one. Submissions created by external
/// tooling (or by Classroom before the migration) keep working, so nothing downstream may assume that a
/// <see cref="Submission"/> has an assignment behind it.
/// </summary>
public class Assignment : ICourseScoped
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    /// <summary>Shown to the student on the accept screen ("Accept the assignment — {Name}").</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// The template repository students are given a copy of, as full "owner/name", normalized with
    /// <see cref="Normalize.RepoName"/>. It must be marked <c>is_template</c> on GitHub.
    /// </summary>
    public string TemplateRepoName { get; set; } = string.Empty;

    /// <summary>
    /// Random, unguessable segment of the invite URL (<c>/{course}/invite/{token}</c>). A readable slug would
    /// let any signed-in user guess another course's assignment and provision themselves a repository, so the
    /// link itself is the capability. Regenerating it invalidates every copy already handed out.
    /// </summary>
    public string InviteToken { get; set; } = string.Empty;

    /// <summary>
    /// Set when the assignment is archived. Archived assignments drop out of the default listing *and* stop
    /// accepting new students; those who already accepted keep their repository link.
    /// </summary>
    public DateTimeOffset? ArchivedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<AssignmentAcceptance> Acceptances { get; } = new List<AssignmentAcceptance>();
}
