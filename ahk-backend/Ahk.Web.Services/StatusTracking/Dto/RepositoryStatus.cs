namespace Ahk.Web.Services.StatusTracking.Dto;

/// <summary>
/// Projected current state of one submission, derived from its append-only event log. Shape preserved from the
/// original <c>grade-management/.../StatusTracking/Dto/RepositoryStatus.cs</c> so the teacher dashboard renders
/// the same information.
/// </summary>
public sealed class RepositoryStatus
{
    /// <summary>Identifies the row for the archive/reactivate actions; the repository name is not an id.</summary>
    public int SubmissionId { get; set; }

    public string Repository { get; set; } = string.Empty;

    /// <summary>When set, the submission is archived. Only present when archived rows were asked for.</summary>
    public DateTimeOffset? ArchivedAt { get; set; }

    public string Neptun { get; set; } = string.Empty;

    /// <summary>
    /// The assignment this repository belongs to, or null when nothing links it to one. Additive to the
    /// legacy shape: the association comes from an <c>AssignmentAcceptance</c> naming the same repository, so a
    /// repository created outside the portal has none.
    /// </summary>
    public int? AssignmentId { get; set; }

    public string? AssignmentName { get; set; }

    public IReadOnlyCollection<string> Branches { get; set; } = Array.Empty<string>();

    public IReadOnlyCollection<PullRequestStatus> PullRequests { get; set; } = Array.Empty<PullRequestStatus>();

    public WorkflowRunsStatus WorkflowRuns { get; set; } = new();
}

public sealed class PullRequestStatus
{
    public int Number { get; set; }

    public string? HtmlUrl { get; set; }

    /// <summary>The most recent action seen for this pull request.</summary>
    public string? Status { get; set; }

    /// <summary>Distinct assignees across the PR's events, comma-joined (as in the original implementation).</summary>
    public string? Assignee { get; set; }
}

public sealed class WorkflowRunsStatus
{
    public int Count { get; set; }

    public string? LastStatus { get; set; }
}
