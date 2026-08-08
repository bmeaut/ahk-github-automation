using System.Diagnostics.CodeAnalysis;

namespace Ahk.Web.Services.GitHubWebhooks;

/// <summary>
/// What each handler made of one delivery, returned as the webhook's response body. This is what an
/// administrator reads in the GitHub App's <em>Advanced → Recent Deliveries</em> tab, which is the only
/// diagnostic surface GitHub offers — so the message strings are a user interface and are kept verbatim from
/// <c>github-monitor</c>.
/// </summary>
public sealed class WebhookResult
{
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Result object is JSON serialized.")]
    public List<string> Messages { get; } = new();

    public void LogInfo(string message) => Messages.Add(message);

    public void LogError(Exception ex, string message) => Messages.Add(message + ": " + ex?.ToString());
}
