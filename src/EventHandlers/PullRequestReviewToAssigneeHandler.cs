using System;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class PullRequestReviewToAssigneeHandler : RepositoryEventBase<PullRequestEventPayload>
    {
        public const string GitHubWebhookEventName = "pull_request";

        public PullRequestReviewToAssigneeHandler(Services.IGitHubClientFactory gitHubClientFactory)
            : base(gitHubClientFactory)
        {
        }

        protected override async Task execute(PullRequestEventPayload webhookPayload, RepositorySettings repoSettings, WebhookResult webhookResult)
        {
            if (webhookPayload.PullRequest == null)
            {
                webhookResult.LogError("no pull request information in webhook payload");
            }
            else if (repoSettings.ReviewerToAssignee == null || !repoSettings.ReviewerToAssignee.Enabled)
            {
                webhookResult.LogError("reviewer to assignee not enabled for repository");
            }
            else if (webhookPayload.Action.Equals("review_requested", StringComparison.OrdinalIgnoreCase))
            {
                if (webhookPayload.PullRequest.RequestedReviewers == null || webhookPayload.PullRequest.RequestedReviewers.Count == 0)
                {
                    webhookResult.LogError("no requested reviewer in webhook payload");
                }
                else if (!isPrAssignedToReviewer(webhookPayload))
                {
                    await GitHubClient.Issue.Assignee.AddAssignees(webhookPayload.Repository.Owner.Login, webhookPayload.Repository.Name, webhookPayload.PullRequest.Number, getUsersToAssign(webhookPayload));
                    webhookResult.LogInfo("pull request review_requested handled, assignee set");
                }
                else
                {
                    webhookResult.LogInfo("pull request review_requested is ok, assignee is present");
                }
            }
            else
            {
                webhookResult.LogInfo($"pull request action {webhookPayload.Action} is not of interrest");
            }
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
