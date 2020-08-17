using System;
using System.Threading.Tasks;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class BranchCreatedEventHandler : RepositoryEventBase<CreateEventPayload>
    {
        public const string GitHubWebhookEventName = "create";

        public BranchCreatedEventHandler(Services.IGitHubClientFactory gitHubClientFactory)
            : base(gitHubClientFactory)
        {
        }

        protected override async Task execute(GitHubClient gitHubClient, CreateEventPayload webhookPayload, RepositorySettings repoSettings, WebhookResult webhookResult)
        {
            if (!webhookPayload.RefType.StringValue.Equals("branch", StringComparison.OrdinalIgnoreCase))
            {
                webhookResult.LogInfo($"create event for ref {webhookPayload.RefType} is not of interrest");
            }
            else if (repoSettings.BranchProtection == null || !repoSettings.BranchProtection.Enabled)
            {
                webhookResult.LogInfo($"branch protection not enabled for repository");
            }
            else
            {
                await gitHubClient.Repository.Branch.UpdateBranchProtection(
                            webhookPayload.Repository.Id, webhookPayload.Ref, getBranchProtectionSettingsUpdate(webhookPayload.Ref));
                webhookResult.LogInfo("Branch protection rule applied.");
            }
        }

        private static BranchProtectionSettingsUpdate getBranchProtectionSettingsUpdate(string branchName)
        {
            // For master: prohibits the merge request into master to be merged
            // For other branches: disables force push
            return new BranchProtectionSettingsUpdate(
                                    requiredStatusChecks: null, // Required. Require status checks to pass before merging. Set to null to disable.
                                    requiredPullRequestReviews: getBranchProtectionRequiredReviewsUpdate(branchName),
                                    restrictions: null, // Push access restrictions. Null to disable.
                                    enforceAdmins: false); // Required. Enforce all configured restrictions for administrators. Set to true to enforce required status checks for repository administrators. Set to null to disable.
        }

        private static BranchProtectionRequiredReviewsUpdate getBranchProtectionRequiredReviewsUpdate(string branchName)
        {
            if (branchName.Equals("master", StringComparison.OrdinalIgnoreCase))
                return new BranchProtectionRequiredReviewsUpdate(false, false, 1); // Prohibits the student from merging the pull request.
            else
                return null;
        }
    }
}
