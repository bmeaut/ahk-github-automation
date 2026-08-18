using Ahk.Web.Services.GitHubWebhooks;
using Microsoft.Extensions.Options;

namespace Ahk.Web.Server.Integrations;

/// <summary>
/// Drains the webhook delivery queue. This is the half of the receiver that GitHub is not waiting on: the
/// controller verifies the signature, records the delivery and answers 202 in milliseconds, and everything
/// that talks to GitHub happens here, with no ten-second deadline over it.
///
/// <para>⚠️ A queue only drains while the process lives. On IIS an application pool that idles out will leave
/// deliveries waiting until the next request wakes the process — the failure is <em>late</em>, not lost, since
/// this service starts with the host and sweeps the backlog immediately. <c>WebhookQueueHealthCheck</c> is the
/// alarm for it, and the deployment note in <c>docs/github-app.md</c> is the fix.</para>
/// </summary>
public sealed class GitHubWebhookDeliveryWorker : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly TimeProvider timeProvider;
    private readonly WebhookOptions options;
    private readonly ILogger<GitHubWebhookDeliveryWorker> logger;

    public GitHubWebhookDeliveryWorker(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<WebhookOptions> options,
        ILogger<GitHubWebhookDeliveryWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.timeProvider = timeProvider;
        this.options = options?.Value ?? new WebhookOptions();
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GitHub webhook delivery worker started, polling every {PollInterval}", options.PollInterval);

        await RunInScopeAsync(p => p.SweepInterruptedAsync(stoppingToken), "startup sweep", stoppingToken);

        var nextRetention = timeProvider.GetUtcNow();

        while (!stoppingToken.IsCancellationRequested)
        {
            // One scope per delivery: the processor and everything it reaches are scoped services over a
            // request-shaped DbContext, and a long-lived one would accumulate tracked entities forever.
            var processed = await RunInScopeAsync(p => p.ProcessNextAsync(stoppingToken), "processing", stoppingToken);

            // Keep going while there is a backlog; only sleep once the queue is empty.
            if (processed == true)
                continue;

            if (timeProvider.GetUtcNow() >= nextRetention)
            {
                await RunInScopeAsync(p => p.RunRetentionAsync(stoppingToken), "retention", stoppingToken);
                nextRetention = timeProvider.GetUtcNow().Add(options.RetentionInterval);
            }

            try
            {
                await Task.Delay(options.PollInterval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("GitHub webhook delivery worker stopped");
    }

    private async Task<T?> RunInScopeAsync<T>(Func<IGitHubWebhookDeliveryProcessor, Task<T>> work, string what, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IGitHubWebhookDeliveryProcessor>();
            return await work(processor);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return default;
        }
#pragma warning disable CA1031 // The loop must survive anything: a database outage must not kill the worker.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "GitHub webhook delivery worker failed during {What}", what);
            return default;
        }
    }
}
