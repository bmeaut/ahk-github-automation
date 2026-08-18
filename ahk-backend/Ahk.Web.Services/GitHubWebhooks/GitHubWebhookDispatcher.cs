using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Ahk.Web.Services.GitHubWebhooks;

public interface IGitHubWebhookDispatcher
{
    /// <summary>
    /// Whether any handler subscribes to this event at all. Asked before a delivery is set up, so that an
    /// event nobody handles — <c>ping</c>, most of all, which GitHub sends when a webhook is first configured
    /// — costs no installation token and, more to the point, does not show up in the delivery log as a
    /// credentials failure on a course that was never going to act on it.
    /// </summary>
    bool HasHandlersFor(string gitHubEventName);

    /// <summary>
    /// Runs every handler subscribed to the delivery's event, in order, and returns what each made of it.
    /// An empty result means no handler subscribes to the event.
    /// </summary>
    /// <param name="context">The delivery. Its body has already been signature-verified.</param>
    /// <param name="onProgress">
    /// Invoked after each handler with the outcomes so far. The worker persists them here rather than at the
    /// end, so that a process killed mid-delivery still leaves a record of what already ran.
    /// </param>
    /// <param name="skipHandlers">
    /// Handler type names to leave alone, by <see cref="WebhookHandlerOutcome.HandlerName"/>. Used by an
    /// administrator re-running a failed delivery: handlers that already succeeded must not act twice.
    /// </param>
    Task<IReadOnlyList<WebhookHandlerOutcome>> ProcessAsync(
        GitHubWebhookContext context,
        Func<IReadOnlyList<WebhookHandlerOutcome>, CancellationToken, Task>? onProgress = null,
        IReadOnlySet<string>? skipHandlers = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Routes a delivery to every handler subscribed to its event name. Port of
/// <c>github-monitor/.../Services/EventDispatch/EventDispatchService.cs</c>.
///
/// Two things carried over deliberately: handlers run <strong>sequentially in registration order</strong> (some
/// post comments and the order they appear in matters), and each is wrapped in its own try/catch so one
/// failing handler does not cost the others their run. The reflective
/// <c>ActivatorUtilities.CreateInstance</c> is gone — it only existed to hand the Azure Function's
/// per-invocation logger to a handler, which plain DI does for free.
///
/// <para>⚠️ Because handler exceptions are swallowed here, "ProcessAsync returned without throwing" says
/// nothing about success. Callers must read the returned outcomes.</para>
/// </summary>
internal sealed class GitHubWebhookDispatcher : IGitHubWebhookDispatcher
{
    private readonly IReadOnlyList<IGitHubWebhookHandler> handlers;
    private readonly ILogger<GitHubWebhookDispatcher> logger;

    public GitHubWebhookDispatcher(IEnumerable<IGitHubWebhookHandler> handlers, ILogger<GitHubWebhookDispatcher> logger)
    {
        this.handlers = handlers.ToList();
        this.logger = logger;
    }

    public bool HasHandlersFor(string gitHubEventName) =>
        handlers.Any(h => string.Equals(h.GitHubEventName, gitHubEventName, StringComparison.OrdinalIgnoreCase));

    public async Task<IReadOnlyList<WebhookHandlerOutcome>> ProcessAsync(
        GitHubWebhookContext context,
        Func<IReadOnlyList<WebhookHandlerOutcome>, CancellationToken, Task>? onProgress = null,
        IReadOnlySet<string>? skipHandlers = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var forEvent = handlers
            .Where(h => string.Equals(h.GitHubEventName, context.GitHubEventName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var outcomes = new List<WebhookHandlerOutcome>();

        if (forEvent.Count == 0)
        {
            logger.LogInformation("Event {EventName} is not of interest", context.GitHubEventName);
            return outcomes;
        }

        for (var order = 0; order < forEvent.Count; order++)
        {
            var handler = forEvent[order];
            var name = handler.GetType().Name;

            if (skipHandlers is not null && skipHandlers.Contains(name))
            {
                logger.LogInformation("Event {EventName}: {Handler} skipped, it already succeeded", context.GitHubEventName, name);
                continue;
            }

            logger.LogInformation("Event {EventName} being handled by {Handler}", context.GitHubEventName, name);

            var started = Stopwatch.GetTimestamp();
            WebhookHandlerOutcome outcome;
            try
            {
                var handlerResult = await handler.ExecuteAsync(context, cancellationToken);
                logger.LogInformation("{Handler} result: {Result}", name, handlerResult.Result);
                outcome = new WebhookHandlerOutcome(name, order, handlerResult.Result, null, ElapsedMs(started));
            }
#pragma warning disable CA1031 // One handler's failure must not cost the others their run.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(ex, "{Handler} execution failed", name);
                outcome = new WebhookHandlerOutcome(name, order, null, ex.ToString(), ElapsedMs(started));
            }

            outcomes.Add(outcome);

            if (onProgress is not null)
                await onProgress(outcomes, cancellationToken);
        }

        return outcomes;
    }

    private static int ElapsedMs(long startedTimestamp) =>
        (int)Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds;
}
