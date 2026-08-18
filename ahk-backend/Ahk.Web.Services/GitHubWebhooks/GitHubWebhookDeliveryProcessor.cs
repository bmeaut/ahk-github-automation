using System.Text.Json;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.GitHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ahk.Web.Services.GitHubWebhooks;

public interface IGitHubWebhookDeliveryProcessor
{
    /// <summary>
    /// Claims the oldest due delivery and runs it. Returns false when the queue is empty, which is the
    /// worker's signal to go and sleep.
    /// </summary>
    Task<bool> ProcessNextAsync(CancellationToken stoppingToken = default);

    /// <summary>
    /// Moves every row still marked <see cref="GitHubWebhookDeliveryStatus.Processing"/> to
    /// <see cref="GitHubWebhookDeliveryStatus.Interrupted"/>. Run once at startup: a single worker in a single
    /// process means such a row can only be the wreckage of a run that was killed.
    /// </summary>
    Task<int> SweepInterruptedAsync(CancellationToken cancellationToken = default);

    /// <summary>Drops old payloads, then deletes old rows. Returns the number of rows deleted.</summary>
    Task<int> RunRetentionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Runs one queued webhook delivery: rebuilds everything the handlers need, dispatches, and records what
/// happened on the delivery row.
///
/// <para>This is the work that used to happen inside the HTTP request, where GitHub's ten-second delivery
/// deadline made a <c>pull_request</c> event on a busy repository a coin toss. Nothing here is on a deadline
/// any more — only on the soft <see cref="WebhookOptions.DeliveryTimeout"/> budget.</para>
/// </summary>
public sealed class GitHubWebhookDeliveryProcessor : IGitHubWebhookDeliveryProcessor
{
    private readonly ApplicationDbContext db;
    private readonly ICourseGitHubAppTokenProvider tokenProvider;
    private readonly ICourseGitHubClientFactory clientFactory;
    private readonly IGitHubWebhookDispatcher dispatcher;
    private readonly TimeProvider timeProvider;
    private readonly WebhookOptions options;
    private readonly ILogger<GitHubWebhookDeliveryProcessor> logger;

    public GitHubWebhookDeliveryProcessor(
        ApplicationDbContext db,
        ICourseGitHubAppTokenProvider tokenProvider,
        ICourseGitHubClientFactory clientFactory,
        IGitHubWebhookDispatcher dispatcher,
        TimeProvider timeProvider,
        IOptions<WebhookOptions> options,
        ILogger<GitHubWebhookDeliveryProcessor> logger)
    {
        this.db = db;
        this.tokenProvider = tokenProvider;
        this.clientFactory = clientFactory;
        this.dispatcher = dispatcher;
        this.timeProvider = timeProvider;
        this.options = options?.Value ?? new WebhookOptions();
        this.logger = logger;
    }

    public async Task<bool> ProcessNextAsync(CancellationToken stoppingToken = default)
    {
        var now = timeProvider.GetUtcNow();

        // ⚠️ Plain select-then-update, with no lease and no locking hint. That is sound only because a single
        // worker in a single process is the only claimer: the admin re-run endpoint moves *terminal* rows back
        // to Pending and refuses a row that is Processing, so it cannot collide either. If the portal is ever
        // scaled out, or the worker's concurrency raised above one, this needs a real atomic claim
        // (UPDATE ... OUTPUT with UPDLOCK/READPAST) and the handlers need re-examining for ordering.
        var delivery = await db.GitHubWebhookDeliveries
            .Where(d => d.Status == GitHubWebhookDeliveryStatus.Pending
                        && (d.NextAttemptAt == null || d.NextAttemptAt <= now))
            .OrderBy(d => d.Id)
            .FirstOrDefaultAsync(stoppingToken);

        if (delivery is null)
            return false;

        delivery.Status = GitHubWebhookDeliveryStatus.Processing;
        delivery.StartedAt = now;
        delivery.AttemptCount++;
        delivery.NextAttemptAt = null;
        await db.SaveChangesAsync(stoppingToken);

        await ProcessAsync(delivery, stoppingToken);
        return true;
    }

    public async Task<int> SweepInterruptedAsync(CancellationToken cancellationToken = default)
    {
        var stranded = await db.GitHubWebhookDeliveries
            .Where(d => d.Status == GitHubWebhookDeliveryStatus.Processing)
            .ToListAsync(cancellationToken);

        if (stranded.Count == 0)
            return 0;

        foreach (var delivery in stranded)
        {
            // Terminal, and never resumed on its own: whatever handlers had already run did so for real, and
            // re-running them would post duplicate comments or merge an already-merged pull request. An
            // administrator decides, from the outcomes recorded so far.
            delivery.Status = GitHubWebhookDeliveryStatus.Interrupted;
            delivery.CompletedAt = timeProvider.GetUtcNow();
            delivery.Error = "The application stopped while this delivery was being processed.";
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Marked {Count} interrupted webhook deliveries at startup", stranded.Count);
        return stranded.Count;
    }

    public async Task<int> RunRetentionAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var payloadCutoff = now.AddDays(-options.PayloadRetentionDays);
        var rowCutoff = now.AddDays(-options.DeliveryRetentionDays);

        // Both passes work in batches. `ExecuteUpdate`/`ExecuteDelete` would express this in one statement each
        // and never load a payload, but they are relational-only, which would leave the whole of retention
        // untested on the in-memory provider CI uses. This runs once a day over rows nobody is waiting for, so
        // the batches are the cheaper trade.
        var dropped = await InBatchesAsync(
            () => TerminalDeliveries()
                .Where(d => d.ReceivedAt < payloadCutoff && d.Payload != null)
                .OrderBy(d => d.Id)
                .Take(200),
            batch =>
            {
                foreach (var delivery in batch)
                    delivery.Payload = null;
            },
            cancellationToken);

        if (dropped > 0)
            logger.LogInformation("Retention: dropped the payload of {Count} webhook deliveries", dropped);

        var deleted = await InBatchesAsync(
            () => TerminalDeliveries()
                .Where(d => d.ReceivedAt < rowCutoff)
                .OrderBy(d => d.Id)
                .Take(500),
            batch => db.GitHubWebhookDeliveries.RemoveRange(batch),
            cancellationToken);

        if (deleted > 0)
            logger.LogInformation("Retention: deleted {Count} webhook deliveries", deleted);

        return deleted;
    }

    /// <summary>
    /// Rows the worker will not move again. Pending and Processing are excluded from retention at any age —
    /// the payload of a delivery that has not run is the work itself. Written out rather than calling a
    /// predicate method, which EF cannot translate.
    /// </summary>
    private IQueryable<GitHubWebhookDelivery> TerminalDeliveries() =>
        db.GitHubWebhookDeliveries.Where(d =>
            d.Status != GitHubWebhookDeliveryStatus.Pending
            && d.Status != GitHubWebhookDeliveryStatus.Processing);

    private async Task<int> InBatchesAsync(
        Func<IQueryable<GitHubWebhookDelivery>> batchQuery,
        Action<List<GitHubWebhookDelivery>> apply,
        CancellationToken cancellationToken)
    {
        var total = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = await batchQuery().ToListAsync(cancellationToken);
            if (batch.Count == 0)
                break;

            apply(batch);
            await db.SaveChangesAsync(cancellationToken);
            total += batch.Count;
        }

        return total;
    }

    /// <summary>
    /// Runs one already-claimed delivery to a terminal state. Internal so the processor tests can drive it
    /// without going through the claim.
    /// </summary>
    internal async Task ProcessAsync(GitHubWebhookDelivery delivery, CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        // A re-run must not make a handler that already worked act twice — there is no un-posting a comment or
        // un-merging a pull request. The skip-set is simply what the row already records as successful, which
        // is why the admin endpoint expresses "re-run everything" by clearing the outcomes rather than by
        // passing a flag down here. Empty on a first attempt, and on an auto-retry, where nothing ran.
        var carried = WebhookHandlerOutcome.ReadList(delivery.OutcomesJson).Where(o => o.Succeeded).ToList();
        var skipHandlers = carried.Select(o => o.HandlerName).ToHashSet(StringComparer.Ordinal);

        // Bounds one wedged delivery without bounding the queue. Soft — see WebhookOptions.DeliveryTimeout.
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        budget.CancelAfter(options.DeliveryTimeout);

        var dispatchStarted = false;

        try
        {
            var age = timeProvider.GetUtcNow() - delivery.ReceivedAt;
            if (age > options.MaxDeliveryAge)
            {
                await CompleteAsync(
                    delivery,
                    GitHubWebhookDeliveryStatus.Skipped,
                    $"Delivery is {age.TotalHours:F1} hours old, older than the {options.MaxDeliveryAge.TotalHours:F0} hour limit, so it was not acted on.");
                return;
            }

            if (string.IsNullOrEmpty(delivery.Payload))
            {
                await CompleteAsync(
                    delivery,
                    GitHubWebhookDeliveryStatus.Failed,
                    "The payload is no longer retained, so this delivery can no longer be processed.");
                return;
            }

            // Asked before anything is set up. GitHub sends `ping` the moment a webhook is configured, and a
            // course with no App credentials would otherwise record it as a credentials failure — filling the
            // delivery log with red for an event nobody was ever going to act on.
            if (!dispatcher.HasHandlersFor(delivery.EventName))
            {
                await CompleteAsync(
                    delivery,
                    GitHubWebhookDeliveryStatus.Skipped,
                    $"Event {delivery.EventName} is not of interest");
                return;
            }

            // No ICurrentCourseProvider is set here, and none is wanted: GitHubWebhookContext states the
            // invariant that nothing on the webhook path relies on the ambient course. Every service the
            // handlers reach takes an explicit course id and reads with IgnoreQueryFilters(), which is what
            // makes running outside a request safe at all. A new service that forgets would read zero rows.

            // Re-read rather than snapshot at accept time: an administrator who turned the integration off in
            // between meant it, and one who corrected the run threshold wants the correction to apply.
            var config = await db.CourseGitHubConfigs.AsNoTracking()
                .FirstOrDefaultAsync(g => g.CourseId == delivery.CourseId, budget.Token);

            if (config is null || !config.Enabled)
            {
                await CompleteAsync(
                    delivery,
                    GitHubWebhookDeliveryStatus.Skipped,
                    "GitHub integration is not configured or is turned off for this course.");
                return;
            }

            var token = await tokenProvider.GetForCourseAsync(delivery.CourseId, bypassCache: false, budget.Token);
            if (token is null)
            {
                // Null means "not configured", never a transport failure — a failed mint throws. So this is a
                // settings problem, and retrying it would only fail identically.
                await CompleteAsync(delivery, GitHubWebhookDeliveryStatus.Failed, "GitHub App ID/Token not configured");
                return;
            }

            var context = new GitHubWebhookContext
            {
                CourseId = delivery.CourseId,
                GitHubEventName = delivery.EventName,

                // Never empty: handlers key SubmissionEvent.GitHubDeliveryId on this, and its unique index
                // treats "" as a value. The synthetic id is stable across re-runs of the same row, so the
                // redelivery guard still works, and unique across rows, so nothing collides.
                DeliveryId = string.IsNullOrEmpty(delivery.DeliveryId) ? $"queue-{delivery.Id}" : delivery.DeliveryId,
                RequestBody = delivery.Payload,
                GitHubClient = clientFactory.CreateForToken(token.Token),
                WorkflowRunThreshold = config.WorkflowRunThreshold,
            };

            dispatchStarted = true;
            var fresh = await dispatcher.ProcessAsync(
                context,
                onProgress: (soFar, ct) => SaveOutcomesAsync(delivery, Merge(carried, soFar), ct),
                skipHandlers: skipHandlers,
                cancellationToken: budget.Token);

            var outcomes = Merge(carried, fresh);

            if (outcomes.Count == 0)
            {
                await CompleteAsync(
                    delivery,
                    GitHubWebhookDeliveryStatus.Skipped,
                    $"Event {delivery.EventName} is not of interest");
                return;
            }

            // ⚠️ The dispatcher swallows handler exceptions, so a clean return says nothing. The outcomes do.
            var failed = outcomes.Count(o => !o.Succeeded);
            await CompleteAsync(
                delivery,
                failed == 0 ? GitHubWebhookDeliveryStatus.Succeeded : GitHubWebhookDeliveryStatus.Failed,
                failed == 0 ? null : $"{failed} of {outcomes.Count} handlers failed.");

            logger.LogInformation(
                "Webhook delivery {QueueId} ({EventName}, {Repository}) finished as {Status}: {Outcomes}",
                delivery.Id,
                delivery.EventName,
                delivery.RepositoryFullName,
                delivery.Status,
                string.Join("; ", outcomes.Select(o => $"{o.HandlerName} -> {o.Result ?? "exception"}")));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is stopping. Leave the row Processing; the next startup sweep will record it as
            // Interrupted, which is exactly what happened.
            logger.LogWarning("Webhook delivery {QueueId} abandoned because the application is stopping", delivery.Id);
        }
#pragma warning disable CA1031 // A delivery's failure is recorded on its row, never thrown at the worker loop.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "Webhook delivery {QueueId} failed", delivery.Id);

            if (dispatchStarted)
            {
                // A handler may already have commented, reviewed or merged. Retrying would do it again, so
                // this waits for a human.
                await CompleteAsync(delivery, GitHubWebhookDeliveryStatus.Failed, Describe(ex));
            }
            else
            {
                // Nothing has touched GitHub yet, so a retry is provably free of side effects.
                await ScheduleRetryOrFailAsync(delivery, ex);
            }
        }
    }

    private static string Describe(Exception ex) => $"{ex.GetType().Name}: {ex.Message}";

    /// <summary>
    /// The outcomes carried over from an earlier attempt, plus this attempt's, back in dispatch order. Without
    /// the carry-over a re-run would erase the record that the skipped handlers ever succeeded, and the *next*
    /// re-run would run them again.
    /// </summary>
    private static List<WebhookHandlerOutcome> Merge(IEnumerable<WebhookHandlerOutcome> carried, IEnumerable<WebhookHandlerOutcome> fresh) =>
        carried.Concat(fresh).OrderBy(o => o.Order).ToList();


    private async Task ScheduleRetryOrFailAsync(GitHubWebhookDelivery delivery, Exception ex)
    {
        if (delivery.AttemptCount >= options.MaxAttempts || options.RetryBackoff.Count == 0)
        {
            await CompleteAsync(delivery, GitHubWebhookDeliveryStatus.Failed, $"Giving up after {delivery.AttemptCount} attempts. {Describe(ex)}");
            return;
        }

        var index = Math.Min(delivery.AttemptCount - 1, options.RetryBackoff.Count - 1);
        var delay = options.RetryBackoff[Math.Max(index, 0)];

        delivery.Status = GitHubWebhookDeliveryStatus.Pending;
        delivery.NextAttemptAt = timeProvider.GetUtcNow().Add(delay);
        delivery.Error = $"Attempt {delivery.AttemptCount} failed before any handler ran, retrying in {delay.TotalMinutes:F0} minutes. {Describe(ex)}";

        // CancellationToken.None: this write is how the delivery stays recoverable.
        await db.SaveChangesAsync(CancellationToken.None);

        logger.LogWarning(
            "Webhook delivery {QueueId} will be retried at {NextAttemptAt} (attempt {Attempt})",
            delivery.Id, delivery.NextAttemptAt, delivery.AttemptCount);
    }

    private async Task CompleteAsync(GitHubWebhookDelivery delivery, GitHubWebhookDeliveryStatus status, string? error)
    {
        delivery.Status = status;
        delivery.CompletedAt = timeProvider.GetUtcNow();
        delivery.NextAttemptAt = null;
        delivery.Error = error;

        // CancellationToken.None: a delivery that ran must not be left looking unprocessed because the token
        // tripped on the way out.
        await db.SaveChangesAsync(CancellationToken.None);
    }

    /// <summary>
    /// Persists the outcomes after every handler rather than once at the end, so a process killed halfway
    /// still leaves the record an administrator needs to decide what to re-run.
    /// </summary>
    private async Task SaveOutcomesAsync(GitHubWebhookDelivery delivery, IReadOnlyList<WebhookHandlerOutcome> outcomes, CancellationToken cancellationToken)
    {
        delivery.OutcomesJson = JsonSerializer.Serialize(outcomes);
        delivery.HandlerCount = outcomes.Count;
        delivery.FailedHandlerCount = outcomes.Count(o => !o.Succeeded);

        await db.SaveChangesAsync(cancellationToken);
    }
}
