namespace Ahk.Web.Services.GitHubWebhooks;

/// <summary>
/// Tuning for the webhook delivery queue and its worker, bound from the <c>Webhooks</c> configuration section.
/// Every default is deliberate; the remarks say why.
/// </summary>
public sealed class WebhookOptions
{
    public const string SectionName = "Webhooks";

    /// <summary>
    /// Whether the background worker runs. Off in tests, which would otherwise poll a database on a timer for
    /// the length of the run.
    /// </summary>
    public bool WorkerEnabled { get; set; } = true;

    /// <summary>
    /// How long the worker waits before looking again once the queue is empty. Short, because it is the delay a
    /// teacher's <c>/ahk ok</c> now waits before anything happens, and one indexed query every couple of
    /// seconds is cheaper than the moving parts of a wake-up signal.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Soft budget for one delivery. Generously above GitHub's ten seconds — being off that deadline is the
    /// point — but bounded so one wedged delivery cannot hold the single worker forever.
    ///
    /// <para>⚠️ Soft: the Octokit methods the handlers call take no <see cref="CancellationToken"/>, so this
    /// cancels <em>between</em> handlers and inside the EF calls, never mid-HTTP-request. A single GitHub call
    /// is bounded only by the Octokit client's own 15-second request timeout.</para>
    /// </summary>
    public TimeSpan DeliveryTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Attempts allowed for failures that happened <em>before</em> any handler ran. Handler failures are never
    /// retried automatically, however many attempts remain — a handler may already have merged a pull request.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Backoff between those attempts, by attempt number. Runs off the end of the list → use the last entry.
    /// </summary>
#pragma warning disable CA1002 // Bound from configuration; a List is what the binder populates.
#pragma warning disable CA2227
    public List<TimeSpan> RetryBackoff { get; set; } = new()
    {
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(25),
    };
#pragma warning restore CA2227
#pragma warning restore CA1002

    /// <summary>
    /// A delivery older than this is skipped rather than run. After an outage the queue holds a backlog, and
    /// acting on a day-old event is worse than not acting: students would get duplicate-PR warnings about pull
    /// requests they opened last week, in a burst that also spends the hourly GitHub rate limit.
    ///
    /// <para>This failure mode is new. Before the queue, a delivery not processed within ten seconds was simply
    /// lost.</para>
    /// </summary>
    public TimeSpan MaxDeliveryAge { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// After this many days a terminal delivery keeps its metadata and outcomes but loses its raw payload.
    /// The payload is the bulk of the row, and it holds commit messages and author emails from private student
    /// repositories — so this is a data-retention decision, not only a disk one.
    /// </summary>
    public int PayloadRetentionDays { get; set; } = 14;

    /// <summary>After this many days a terminal delivery row is deleted outright.</summary>
    public int DeliveryRetentionDays { get; set; } = 90;

    /// <summary>How often the retention pass runs. It only ever runs while the queue is idle.</summary>
    public TimeSpan RetentionInterval { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// The queue health check fails once the oldest waiting delivery is older than this. It is the alarm for a
    /// worker that is not running at all — most plausibly an IIS application pool that idled the process out,
    /// since a queue only drains while the process lives.
    /// </summary>
    public TimeSpan PendingAgeAlarm { get; set; } = TimeSpan.FromMinutes(30);
}
