using Ahk.Web.Services.StatusTracking;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;
using PullRequestStatusEvent = Ahk.Web.Data.Entities.PullRequestEvent;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers.StatusTracking;

/// <summary>
/// Records pull request lifecycle events. The status projection reads the latest action per pull request
/// number, so the four actions tracked here are exactly the ones that change what a teacher sees. Ported from
/// <c>github-monitor/.../EventHandlers/StatusTracking/PullRequestStatusTrackingHandler.cs</c>.
/// </summary>
public sealed class PullRequestStatusTrackingHandler : RepositoryEventHandlerBase<PullRequestEventPayload>, IStatusEventWriter
{
    private readonly ISubmissionEventService events;

    public PullRequestStatusTrackingHandler(ISubmissionEventService events, IMemoryCache cache, ILogger<PullRequestStatusTrackingHandler> logger)
        : base(cache, logger)
    {
        this.events = events;
    }

    public override string GitHubEventName => "pull_request";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(GitHubWebhookContext context, PullRequestEventPayload payload, CancellationToken cancellationToken)
    {
        if (payload.PullRequest is null)
            return EventHandlerResult.PayloadError("no pull request information in webhook payload");

        if (!payload.Action.Equals("opened", StringComparison.OrdinalIgnoreCase)
            && !payload.Action.Equals("assigned", StringComparison.OrdinalIgnoreCase)
            && !payload.Action.Equals("review_requested", StringComparison.OrdinalIgnoreCase)
            && !payload.Action.Equals("closed", StringComparison.OrdinalIgnoreCase))
        {
            return EventHandlerResult.EventNotOfInterest(payload.Action);
        }

        var neptun = await GetNeptunAsync(context, payload.Repository.Id, payload.PullRequest.Head.Ref);

        var submissionEvent = new PullRequestStatusEvent
        {
            Number = payload.PullRequest.Number,
            Action = payload.Action,
            HtmlUrl = payload.PullRequest.HtmlUrl,
            Neptun = neptun,
            Assignees = payload.PullRequest.Assignees?.Select(u => u.Login).ToList() ?? new List<string>(),
            GitHubDeliveryId = BranchCreateStatusTrackingHandler.NullIfEmpty(context.DeliveryId),
            Timestamp = DateTimeOffset.UtcNow,
        };

        // The neptun is passed on as well, not just stored on the event: it is what links the submission to a
        // student row, and a pull request is usually the first place it becomes known.
        var recorded = await events.RecordAsync(context.CourseId, payload.Repository.FullName, submissionEvent, neptun, cancellationToken);
        if (!recorded)
            return EventHandlerResult.NoActionNeeded("redelivery, event already recorded");

        return EventHandlerResult.ActionPerformed("pull request lifecycle handled");
    }
}
