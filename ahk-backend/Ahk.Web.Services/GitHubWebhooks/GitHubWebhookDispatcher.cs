using Microsoft.Extensions.Logging;

namespace Ahk.Web.Services.GitHubWebhooks;

public interface IGitHubWebhookDispatcher
{
    Task ProcessAsync(GitHubWebhookContext context, WebhookResult result, CancellationToken cancellationToken = default);
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

    public async Task ProcessAsync(GitHubWebhookContext context, WebhookResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        var forEvent = handlers
            .Where(h => string.Equals(h.GitHubEventName, context.GitHubEventName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (forEvent.Count == 0)
        {
            result.LogInfo($"Event {context.GitHubEventName} is not of interest");
            logger.LogInformation("Event {EventName} is not of interest", context.GitHubEventName);
            return;
        }

        foreach (var handler in forEvent)
        {
            var name = handler.GetType().Name;
            logger.LogInformation("Event {EventName} being handled by {Handler}", context.GitHubEventName, name);

            try
            {
                var handlerResult = await handler.ExecuteAsync(context, cancellationToken);
                logger.LogInformation("{Handler} result: {Result}", name, handlerResult.Result);
                result.LogInfo($"{name} -> {handlerResult.Result}");
            }
#pragma warning disable CA1031 // One handler's failure must not cost the others their run.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(ex, "{Handler} execution failed", name);
                result.LogError(ex, $"{name} -> exception");
            }
        }
    }
}
