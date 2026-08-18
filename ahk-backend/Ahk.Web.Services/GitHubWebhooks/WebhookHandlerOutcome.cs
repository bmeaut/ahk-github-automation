using System.Text.Json;

namespace Ahk.Web.Services.GitHubWebhooks;

/// <summary>
/// What one handler made of one delivery. Replaces <c>WebhookResult</c>'s flat message list, which used to be
/// the webhook's response body: now that the receiver answers 202 before any handler runs, these are persisted
/// on the delivery row instead and shown in the admin console.
///
/// <para>The strings are unchanged — <see cref="Result"/> is <see cref="EventHandlerResult.Result"/> verbatim,
/// prefixes included ("no action needed: …", "event handler disabled: …") — because they were, and remain, a
/// user interface.</para>
///
/// <para><see cref="HandlerName"/> is <c>handler.GetType().Name</c>, and an admin re-run keys its skip-set on
/// it: renaming a handler class silently stops previously-succeeded handlers from being skipped, which is why
/// <c>WebhookHandlerRegistrationTests</c> pins the relationship.</para>
/// </summary>
/// <param name="HandlerName">The handler type's simple name.</param>
/// <param name="Order">Position in the dispatch order, which is DI registration order.</param>
/// <param name="Result">The handler's verdict, or null when it threw.</param>
/// <param name="Error">The exception, formatted, or null when the handler returned.</param>
/// <param name="DurationMs">Wall-clock time the handler took.</param>
public sealed record WebhookHandlerOutcome(string HandlerName, int Order, string? Result, string? Error, int DurationMs)
{
    public bool Succeeded => Error is null;

    /// <summary>
    /// Reads the list back out of <c>GitHubWebhookDelivery.OutcomesJson</c>. Malformed or absent JSON yields
    /// an empty list rather than an exception: this is a record of what happened, and it is never worth
    /// failing a delivery — or a screen — over.
    /// </summary>
    public static IReadOnlyList<WebhookHandlerOutcome> ReadList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<WebhookHandlerOutcome>();

        try
        {
            return JsonSerializer.Deserialize<List<WebhookHandlerOutcome>>(json) ?? [];
        }
        catch (JsonException)
        {
            return Array.Empty<WebhookHandlerOutcome>();
        }
    }
}
