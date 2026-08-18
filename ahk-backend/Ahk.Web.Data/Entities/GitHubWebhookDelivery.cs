namespace Ahk.Web.Data.Entities;

/// <summary>Where a delivery is in its life. Terminal states are everything except Pending and Processing.</summary>
public enum GitHubWebhookDeliveryStatus
{
    /// <summary>Accepted and waiting for the worker.</summary>
    Pending = 0,

    /// <summary>Claimed by the worker and running.</summary>
    Processing = 1,

    /// <summary>Every subscribed handler returned, including "no action needed" and "disabled".</summary>
    Succeeded = 2,

    /// <summary>A handler threw, or the delivery could not be set up.</summary>
    Failed = 3,

    /// <summary>Nothing to do: no handler for the event, integration turned off, or the delivery was too old.</summary>
    Skipped = 4,

    /// <summary>The process died while this delivery was running. Terminal, and never resumed automatically.</summary>
    Interrupted = 5,
}

/// <summary>
/// One signature-verified GitHub webhook delivery, recorded so it can be processed off the request thread.
///
/// <para>GitHub gives a delivery ten seconds; a single <c>pull_request</c> event can cost the handlers more
/// sequential GitHub API calls than that allows. The receiver therefore verifies the HMAC inline — the
/// signature is the authentication, so an unverified body is never written here — persists the delivery, and
/// answers 202. <c>GitHubWebhookDeliveryWorker</c> drains this table with no deadline.</para>
///
/// <para>⚠️ Deliberately <strong>not</strong> <c>ICourseScoped</c>. Its only two readers — the background
/// worker and the <c>/api/admin/...</c> controller — have no current course, and the global query filter
/// matches nothing when none is resolved. Filtering this entity would make the queue silently read empty,
/// which is the worst possible failure for a queue. It carries a plain <see cref="CourseId"/> instead, like
/// <see cref="CourseGitHubConfig"/>.</para>
/// </summary>
public class GitHubWebhookDelivery
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    /// <summary>
    /// The <c>X-GitHub-Delivery</c> header, or null when it was absent. Null rather than empty on purpose:
    /// handlers key <c>SubmissionEvent.GitHubDeliveryId</c> on it, and that column's unique index is filtered
    /// on <c>IS NOT NULL</c> — so a stored <c>""</c> would let the second header-less delivery collide.
    /// </summary>
    public string? DeliveryId { get; set; }

    /// <summary>The <c>X-GitHub-Event</c> header.</summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>From <c>repository.full_name</c>; already parsed to resolve the course, so it is free to keep.</summary>
    public string RepositoryFullName { get; set; } = string.Empty;

    /// <summary>
    /// The raw, signature-verified request body, verbatim — the handlers deserialize it again. Set to null by
    /// the retention pass once the delivery is old, which is what keeps this table from growing without bound.
    /// </summary>
    public string? Payload { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public GitHubWebhookDeliveryStatus Status { get; set; }

    public int AttemptCount { get; set; }

    /// <summary>When the worker may next claim this row; null means "not scheduled".</summary>
    public DateTimeOffset? NextAttemptAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Delivery-level reason for a non-success status (setup failure, integration off, too old).</summary>
    public string? Error { get; set; }

    /// <summary>
    /// The per-handler outcomes, serialized. A JSON column rather than a child table because they are only
    /// ever read whole, for one delivery, by one screen — and because a queryable child table would need its
    /// own <c>CourseId</c>, which would create a second cascade path from <see cref="Course"/> and drag the
    /// whole <c>NoAction</c> plus explicit-delete arrangement in with it.
    /// </summary>
    public string? OutcomesJson { get; set; }

    /// <summary>Denormalized from <see cref="OutcomesJson"/> so the admin list never deserializes anything.</summary>
    public int HandlerCount { get; set; }

    /// <summary>Denormalized from <see cref="OutcomesJson"/>. See <see cref="HandlerCount"/>.</summary>
    public int FailedHandlerCount { get; set; }

    /// <summary>True once the delivery has reached a state the worker will not move it out of.</summary>
    public bool IsTerminal => Status is not (GitHubWebhookDeliveryStatus.Pending or GitHubWebhookDeliveryStatus.Processing);
}
