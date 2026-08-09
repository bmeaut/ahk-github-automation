using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers;

/// <summary>
/// Mirrors a requested reviewer onto the pull request's assignee, so the teacher dashboard's "who is reviewing
/// this" column is populated by the act of requesting a review. Ported from
/// <c>github-monitor/.../EventHandlers/PullRequestReviewToAssigneeHandler.cs</c>.
/// </summary>
public sealed class PullRequestReviewToAssigneeHandler : RepositoryEventHandlerBase<PullRequestEventPayload>
{
    public PullRequestReviewToAssigneeHandler(IMemoryCache cache, ILogger<PullRequestReviewToAssigneeHandler> logger)
        : base(cache, logger)
    {
    }

    public override string GitHubEventName => "pull_request";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(GitHubWebhookContext context, PullRequestEventPayload payload, CancellationToken cancellationToken)
    {
        if (payload.PullRequest is null)
            return EventHandlerResult.PayloadError("no pull request information in webhook payload");

        if (!payload.Action.Equals("review_requested", StringComparison.OrdinalIgnoreCase))
            return EventHandlerResult.EventNotOfInterest(payload.Action);

        if (payload.PullRequest.RequestedReviewers is null || payload.PullRequest.RequestedReviewers.Count == 0)
            return EventHandlerResult.PayloadError("no requested reviewer in webhook payload");

        if (IsPrAssignedToReviewer(payload))
            return EventHandlerResult.NoActionNeeded("pull request review_requested is ok, assignee is present");

        await context.GitHubClient.Issue.Assignee.AddAssignees(
            payload.Repository.Owner.Login, payload.Repository.Name, payload.PullRequest.Number, GetUsersToAssign(payload));

        return EventHandlerResult.ActionPerformed("pull request review_requested handled, assignee set");
    }

    private static AssigneesUpdate GetUsersToAssign(PullRequestEventPayload payload)
        => new(payload.PullRequest.RequestedReviewers.Select(r => r.Login).ToList());

    private static bool IsPrAssignedToReviewer(PullRequestEventPayload payload)
        => payload.PullRequest.Assignee is not null
            && payload.PullRequest.RequestedReviewers.Any(r => r.Id == payload.PullRequest.Assignee.Id);
}
