namespace Ahk.Web.Data.Entities;

/// <summary>
/// One student's acceptance of one <see cref="Assignment"/>: which repository was created for them and when.
/// The unique index on (AssignmentId, UserId) is what makes accepting twice — a double click, a second tab —
/// produce one repository rather than two.
///
/// The identity link is the <see cref="ApplicationUser"/>, not <see cref="Student"/>: the signed-in account is
/// who accepted, and the course-scoped student row is reachable through their Neptun code when grading needs it.
/// No submission link either — one repository can carry many submissions over a semester, and none of them are
/// this record's business.
/// </summary>
public class AssignmentAcceptance : ICourseScoped
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public int AssignmentId { get; set; }

    public Assignment? Assignment { get; set; }

    /// <summary>The account that clicked Accept.</summary>
    public int UserId { get; set; }

    public ApplicationUser? User { get; set; }

    /// <summary>Full "owner/name" of the created repository, normalized with <see cref="Normalize.RepoName"/>.</summary>
    public string GitHubRepoName { get; set; } = string.Empty;

    public string RepoUrl { get; set; } = string.Empty;

    /// <summary>The GitHub login the repository was shared with, as it stood at accept time.</summary>
    public string GitHubUsername { get; set; } = string.Empty;

    public DateTimeOffset AcceptedAt { get; set; } = DateTimeOffset.UtcNow;

    // --- Collaborator invitation state ---
    // A student who is already an organization member is added to the repository outright (GitHub answers 204).
    // Anyone else only gets an *invitation* (201) which they must accept, and which expires. Until then the
    // repository is invisible to them, so the portal has to track and be able to re-send it.

    /// <summary>True while GitHub has an outstanding invitation the student has not accepted yet.</summary>
    public bool InvitationPending { get; set; }

    /// <summary>GitHub's invitation id — needed to delete the stale one before issuing a replacement.</summary>
    public long? InvitationId { get; set; }

    public DateTimeOffset? InvitationSentAt { get; set; }
}
