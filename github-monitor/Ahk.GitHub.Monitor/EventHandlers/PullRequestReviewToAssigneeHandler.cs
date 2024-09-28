using System;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class PullRequestReviewToAssigneeHandler(
        Services.IGitHubClientFactory gitHubClientFactory,
        Microsoft.Extensions.Caching.Memory.IMemoryCache cache,
        Microsoft.Extensions.Logging.ILogger logger)
        : RepositoryEventBase<PullRequestEventPayload>(gitHubClientFactory, cache, logger)
    {
        public const string GitHubWebhookEventName = "pull_request";

        protected override async Task<EventHandlerResult> executeCore(PullRequestEventPayload webhookPayload)
        {
            if (webhookPayload.PullRequest == null)
                return EventHandlerResult.PayloadError("no pull request information in webhook payload");

            if (webhookPayload.Action.Equals("review_requested", StringComparison.OrdinalIgnoreCase))
            {
                if (webhookPayload.PullRequest.RequestedReviewers == null || webhookPayload.PullRequest.RequestedReviewers.Count == 0)
                {
                    return EventHandlerResult.PayloadError("no requested reviewer in webhook payload");
                }

                if (!isPrAssignedToReviewer(webhookPayload))
                {
                    await this.GitHubClient.Issue.Assignee.AddAssignees(webhookPayload.Repository.Owner.Login, webhookPayload.Repository.Name, webhookPayload.PullRequest.Number, getUsersToAssign(webhookPayload));
                    return EventHandlerResult.ActionPerformed("pull request review_requested handled, assignee set");
                }

                return EventHandlerResult.NoActionNeeded("pull request review_requested is ok, assignee is present");
            }

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }

        private static AssigneesUpdate getUsersToAssign(PullRequestEventPayload webhookPayload)
            => new AssigneesUpdate(webhookPayload.PullRequest.RequestedReviewers.Select(r => r.Login).ToList());

        private static bool isPrAssignedToReviewer(PullRequestEventPayload webhookPayload)
        {
            if (webhookPayload.PullRequest.Assignee == null)
                return false;

            return webhookPayload.PullRequest.RequestedReviewers.Any(r => r.Id == webhookPayload.PullRequest.Assignee.Id);
        }
    }
}
