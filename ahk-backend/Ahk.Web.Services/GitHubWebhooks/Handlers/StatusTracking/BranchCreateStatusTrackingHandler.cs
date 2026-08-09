using Ahk.Web.Data.Entities;
using Ahk.Web.Services.StatusTracking;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers.StatusTracking;

/// <summary>
/// Records branch creation in the submission event log. Ported from
/// <c>github-monitor/.../EventHandlers/StatusTracking/BranchCreateStatusTrackingHandler.cs</c>.
///
/// <para>The two outcomes are mutually exclusive, which is what keeps the one-status-event-per-delivery
/// invariant intact for the <c>create</c> event.</para>
/// </summary>
public sealed class BranchCreateStatusTrackingHandler : RepositoryEventHandlerBase<CreateEventPayload>, IStatusEventWriter
{
    private readonly ISubmissionEventService events;

    public BranchCreateStatusTrackingHandler(ISubmissionEventService events, IMemoryCache cache, ILogger<BranchCreateStatusTrackingHandler> logger)
        : base(cache, logger)
    {
        this.events = events;
    }

    public override string GitHubEventName => "create";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(GitHubWebhookContext context, CreateEventPayload payload, CancellationToken cancellationToken)
    {
        if (!payload.RefType.Equals(RefType.Branch))
            return EventHandlerResult.EventNotOfInterest($"branch create ignored for RefType: {payload.RefType}, Ref: {payload.Ref}");

        // Repository creation is recognised here rather than from the dedicated `repository` event: at that
        // point the repository is still empty and carries no ahk-monitor.yml, so the opt-in gate would reject
        // it. Creation of the default branch is the first moment the repository is recognisable as ours.
        var isRepositoryCreate = payload.Ref.Equals(payload.Repository.DefaultBranch, StringComparison.OrdinalIgnoreCase);

        SubmissionEvent submissionEvent = isRepositoryCreate
            ? new RepositoryCreatedEvent()
            : new BranchCreatedEvent { Branch = payload.Ref };

        submissionEvent.GitHubDeliveryId = NullIfEmpty(context.DeliveryId);
        submissionEvent.Timestamp = DateTimeOffset.UtcNow;

        var recorded = await events.RecordAsync(context.CourseId, payload.Repository.FullName, submissionEvent, cancellationToken: cancellationToken);
        if (!recorded)
            return EventHandlerResult.NoActionNeeded("redelivery, event already recorded");

        return EventHandlerResult.ActionPerformed(
            isRepositoryCreate ? "repository create lifecycle handled" : "branch create lifecycle handled");
    }

    internal static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
