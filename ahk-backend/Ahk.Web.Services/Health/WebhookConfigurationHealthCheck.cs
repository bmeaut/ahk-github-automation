using Ahk.Web.Data.Entities;

namespace Ahk.Web.Services.Health;

/// <summary>
/// Checks the settings the GitHub webhook receiver will need: an organization to resolve incoming deliveries
/// to this course, and a secret to validate their X-Hub-Signature-256 header. Local check, no network.
/// </summary>
public sealed class WebhookConfigurationHealthCheck : ICourseHealthCheck
{
    public string Id => "github-webhook-config";

    public string Title => "Webhook settings";

    public int Order => 10;

    public Task<HealthCheckResult> RunAsync(Course course, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(course.GitHubOrganization))
        {
            return Task.FromResult(HealthCheckResult.NotConfigured(
                this,
                "The course has no GitHub organization, so incoming deliveries cannot be routed to it.",
                "Set the GitHub organization on the course's general settings."));
        }

        var config = course.GitHubConfig;
        if (config is null || string.IsNullOrWhiteSpace(config.GitHubWebhookSecret))
        {
            return Task.FromResult(HealthCheckResult.Warning(
                this,
                $"Deliveries route to '{course.GitHubOrganization}', but no webhook secret is stored, so their signature cannot be validated.",
                "Paste the secret configured on the GitHub App into GitHub integration."));
        }

        if (!config.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Warning(
                this,
                "The integration is turned off — the portal will ignore this course's webhooks.",
                "Turn the integration back on under GitHub integration."));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            this,
            $"Deliveries from '{course.GitHubOrganization}' route to this course and their signature is validated."));
    }
}
