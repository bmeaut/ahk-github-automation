namespace Ahk.Web.Services.GitHubWebhooks;

/// <summary>
/// One handler's verdict on one delivery. The prefixes ("payload error", "no action needed", …) are what shows
/// up in the delivery log, so they are kept verbatim from <c>github-monitor</c>.
/// </summary>
public sealed class EventHandlerResult
{
    public EventHandlerResult(string result) => this.Result = result;

    public string Result { get; }

    public static EventHandlerResult PayloadError(string message) => new($"payload error: {message}");

    public static EventHandlerResult NoActionNeeded(string message) => new($"no action needed: {message}");

    public static EventHandlerResult ActionPerformed(string message) => new($"action performed: {message}");

    public static EventHandlerResult EventNotOfInterest(string action) => new($"action not of interest: {action}");

    public static EventHandlerResult Disabled(string? message = null) => new(message is null ? "event handler disabled" : $"event handler disabled: {message}");
}
