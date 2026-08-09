using Ahk.Web.Services.GitHubWebhooks.Payloads;
using Ahk.Web.Services.StatusTracking;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WorkflowRunStatusEvent = Ahk.Web.Data.Entities.WorkflowRunEvent;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers.StatusTracking;

/// <summary>
/// Records each completed evaluation run, which is what the dashboard's run count and last-conclusion columns
/// project over. Ported from
/// <c>github-monitor/.../EventHandlers/StatusTracking/WorkflowRunStatusTrackingHandler.cs</c>.
/// </summary>
public sealed class WorkflowRunStatusTrackingHandler : RepositoryEventHandlerBase<WorkflowEventPayload>, IStatusEventWriter
{
    private readonly ISubmissionEventService events;

    public WorkflowRunStatusTrackingHandler(ISubmissionEventService events, IMemoryCache cache, ILogger<WorkflowRunStatusTrackingHandler> logger)
        : base(cache, logger)
    {
        this.events = events;
    }

    public override string GitHubEventName => "workflow_run";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(GitHubWebhookContext context, WorkflowEventPayload payload, CancellationToken cancellationToken)
    {
        if (payload.WorkflowRun is null)
            return EventHandlerResult.PayloadError("no workflow run information in webhook payload");

        if (payload.Action is null || !payload.Action.Equals("completed", StringComparison.OrdinalIgnoreCase))
            return EventHandlerResult.EventNotOfInterest(payload.Action ?? string.Empty);

        var submissionEvent = new WorkflowRunStatusEvent
        {
            Conclusion = payload.WorkflowRun.Conclusion,
            GitHubDeliveryId = BranchCreateStatusTrackingHandler.NullIfEmpty(context.DeliveryId),
            Timestamp = DateTimeOffset.UtcNow,
        };

        var recorded = await events.RecordAsync(context.CourseId, payload.Repository.FullName, submissionEvent, cancellationToken: cancellationToken);
        if (!recorded)
            return EventHandlerResult.NoActionNeeded("redelivery, event already recorded");

        return EventHandlerResult.ActionPerformed("workflow_run lifecycle handled");
    }
}
