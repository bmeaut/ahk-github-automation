using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers.Abstractions;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Event is a GitHub terminology in this context.")]
public interface IGitHubEventHandler
{
    public static abstract string GitHubWebhookEventName { get; }
    public Task<EventHandlerResult> ExecuteAsync(string requestBody);
}
