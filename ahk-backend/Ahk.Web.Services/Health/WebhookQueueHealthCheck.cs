using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.GitHubWebhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ahk.Web.Services.Health;

/// <summary>
/// Reports on the course's webhook delivery queue.
///
/// <para>This check exists because the receiver now answers <c>202 Accepted</c> before any handler runs: a
/// course whose GitHub App credentials are wrong used to show up as a red 500 in GitHub's own delivery log,
/// and now shows up as a green 202 whose failure lives only in our database. It is also the alarm for a worker
/// that is not running at all — most plausibly an IIS application pool that idled the process out — which is
/// what the oldest-waiting-delivery age detects.</para>
///
/// <para>A local database read only; unlike the two GitHub checks it adds nothing meaningful to the admin
/// dashboard's cost.</para>
/// </summary>
public sealed class WebhookQueueHealthCheck : ICourseHealthCheck
{
    private readonly ApplicationDbContext db;
    private readonly TimeProvider timeProvider;
    private readonly WebhookOptions options;

    public WebhookQueueHealthCheck(ApplicationDbContext db, TimeProvider timeProvider, IOptions<WebhookOptions> options)
    {
        this.db = db;
        this.timeProvider = timeProvider;
        this.options = options?.Value ?? new WebhookOptions();
    }

    public string Id => "webhook-queue";

    public string Title => "Webhook delivery queue";

    public int Order => 40;

    public async Task<HealthCheckResult> RunAsync(Course course, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(course);

        var now = timeProvider.GetUtcNow();
        var since = now.AddHours(-24);

        // GitHubWebhookDelivery carries no query filter, so this reads what it says it reads. Filtering on the
        // course explicitly, as every service here does.
        var recent = await db.GitHubWebhookDeliveries
            .AsNoTracking()
            .Where(d => d.CourseId == course.Id)
            .Where(d => d.ReceivedAt >= since || d.Status == GitHubWebhookDeliveryStatus.Pending)
            .Select(d => new { d.Status, d.ReceivedAt })
            .ToListAsync(cancellationToken);

        if (recent.Count == 0)
            return HealthCheckResult.Healthy(this, "No webhook deliveries in the last 24 hours.");

        var pending = recent.Where(d => d.Status == GitHubWebhookDeliveryStatus.Pending).ToList();
        var failed = recent.Count(d => d.Status is GitHubWebhookDeliveryStatus.Failed or GitHubWebhookDeliveryStatus.Interrupted);

        if (pending.Count > 0)
        {
            var oldest = now - pending.Min(d => d.ReceivedAt);
            if (oldest > options.PendingAgeAlarm)
            {
                return HealthCheckResult.Failed(
                    this,
                    $"{pending.Count} deliveries are waiting, the oldest for {Describe(oldest)}. They are not being processed.",
                    "The queue only drains while the application is running. Check that the IIS application pool is set to always-running with no idle time-out, and that the application has not stopped.");
            }
        }

        if (failed > 0)
        {
            return HealthCheckResult.Warning(
                this,
                $"{failed} of {recent.Count} deliveries in the last 24 hours failed or were interrupted.",
                "Open Site administration → Webhook deliveries to see which handler failed and to re-run it.");
        }

        return pending.Count > 0
            ? HealthCheckResult.Healthy(this, $"{recent.Count} deliveries in the last 24 hours, {pending.Count} waiting to be processed.")
            : HealthCheckResult.Healthy(this, $"{recent.Count} deliveries in the last 24 hours, all processed.");
    }

    private static string Describe(TimeSpan age) => age.TotalHours >= 1
        ? $"{age.TotalHours:F0} hours"
        : $"{age.TotalMinutes:F0} minutes";
}
