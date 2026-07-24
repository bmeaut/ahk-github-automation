namespace Ahk.Web.Data.Entities;

/// <summary>
/// Append-only status event for a submission. Mirrors the polymorphic <c>StatusEventBase</c> log of the
/// original CosmosDB <c>events</c> container, mapped table-per-hierarchy. Rows are never mutated; the
/// current status of a submission is a projection over this log (see the status projection in
/// Ahk.Web.Services), exactly as <c>StatusTrackingService.createStatus</c> did.
/// </summary>
public abstract class SubmissionEvent : ICourseScoped
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public int SubmissionId { get; set; }

    public Submission? Submission { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// GitHub's X-GitHub-Delivery id. Unique (where present) so webhook redeliveries do not duplicate rows —
    /// a guard the original Cosmos model lacked.
    /// </summary>
    public string? GitHubDeliveryId { get; set; }
}

/// <summary>Repository was created for the student (was RepositoryCreateEvent).</summary>
public class RepositoryCreatedEvent : SubmissionEvent
{
}

/// <summary>A branch was pushed/created (was BranchCreateEvent).</summary>
public class BranchCreatedEvent : SubmissionEvent
{
    public string Branch { get; set; } = string.Empty;
}

/// <summary>Pull request activity (was PullRequestEvent).</summary>
public class PullRequestEvent : SubmissionEvent
{
    public int Number { get; set; }

    /// <summary>GitHub action name (opened, closed, assigned, ...); the latest one is the PR's status.</summary>
    public string Action { get; set; } = string.Empty;

    public string? HtmlUrl { get; set; }

    /// <summary>Neptun as seen at event time; kept as a snapshot because the original log recorded it per event.</summary>
    public string? Neptun { get; set; }

    /// <summary>Assignees at event time. Mapped to a JSON column — only ever concatenated for display.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF Core primitive collections require a settable List<T>.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "EF Core primitive collections require List<T>.")]
    public List<string> Assignees { get; set; } = new();
}

/// <summary>An Actions workflow run finished (was WorkflowRunEvent).</summary>
public class WorkflowRunEvent : SubmissionEvent
{
    public string? Conclusion { get; set; }
}
