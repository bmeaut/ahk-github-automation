namespace Ahk.Web.Services.GitHubWebhooks;

/// <summary>
/// One rule enforced, or one fact recorded, in response to a GitHub webhook event. Handlers are registered in
/// DI and selected by <see cref="GitHubEventName"/>; several may subscribe to the same event and each runs
/// independently, so one throwing does not stop the others.
/// </summary>
public interface IGitHubWebhookHandler
{
    /// <summary>The <c>X-GitHub-Event</c> name this handler subscribes to, e.g. <c>pull_request</c>.</summary>
    string GitHubEventName { get; }

    Task<EventHandlerResult> ExecuteAsync(GitHubWebhookContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Marks a handler that appends to the submission event log.
///
/// ⚠️ <c>SubmissionEvent.GitHubDeliveryId</c> is globally unique, but one delivery fans out to several
/// handlers — so <strong>at most one handler per event name may write a status event</strong>, or the second
/// write is silently dropped as a redelivery (and would violate the unique index on SQL Server). The invariant
/// is asserted by <c>WebhookHandlerRegistrationTests</c>; if you need a second writer for an event, key the
/// delivery id per handler first.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "A marker is the point: it is what the registration test can see without running a handler.")]
public interface IStatusEventWriter
{
}
