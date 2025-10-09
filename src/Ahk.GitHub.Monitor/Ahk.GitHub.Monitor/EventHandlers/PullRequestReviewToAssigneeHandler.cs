using System;
using System.Linq;
using System.Threading.Tasks;

using Ahk.GitHub.Monitor.EventHandlers.Abstractions;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers;

public class PullRequestReviewToAssigneeHandler(
    IGitHubClientFactory gitHubClientFactory,
    IMemoryCache cache,
    ILogger<PullRequestReviewToAssigneeHandler> logger)
    : RepositoryEventHandlerBase<PullRequestEventPayload>(gitHubClientFactory, cache, logger), IGitHubEventHandler
{
    public static string GitHubWebhookEventName => "pull_request";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(PullRequestEventPayload webhookPayload)
    {
        if (webhookPayload.PullRequest == null)
        {
            return EventHandlerResult.PayloadError("no pull request information in webhook payload");
        }

        if (webhookPayload.Action.Equals("review_requested", StringComparison.OrdinalIgnoreCase))
        {
            if ((webhookPayload.PullRequest.RequestedReviewers?.Count ?? 0) == 0)
            {
                return EventHandlerResult.PayloadError("no requested reviewer in webhook payload");
            }

            if (!IsPrAssignedToReviewer(webhookPayload))
            {
                await GitHubClient.Issue.Assignee.AddAssignees(
                    webhookPayload.Repository.Owner.Login,
                    webhookPayload.Repository.Name, webhookPayload.PullRequest.Number,
                    new([.. webhookPayload.PullRequest.RequestedReviewers.Select(r => r.Login)]));

                return EventHandlerResult.ActionPerformed("pull request review_requested handled, assignee set");
            }

            return EventHandlerResult.NoActionNeeded("pull request review_requested is ok, assignee is present");
        }

        return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
    }

    private static bool IsPrAssignedToReviewer(PullRequestEventPayload webhookPayload)
    {
        if (webhookPayload.PullRequest.Assignee == null)
        {
            return false;
        }

        return webhookPayload.PullRequest.RequestedReviewers.Any(r => r.Id == webhookPayload.PullRequest.Assignee.Id);
    }
}
