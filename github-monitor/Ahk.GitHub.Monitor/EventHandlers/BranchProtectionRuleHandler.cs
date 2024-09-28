using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class BranchProtectionRuleHandler(
        IGitHubClientFactory gitHubClientFactory,
        IMemoryCache cache,
        ILogger logger)
        : RepositoryEventBase<CreateEventPayload>(gitHubClientFactory, cache, logger)
    {
        public const string GitHubWebhookEventName = "create";

        protected override async Task<EventHandlerResult> executeCore(CreateEventPayload webhookPayload)
        {
            if (!webhookPayload.RefType.StringValue.Equals("branch", StringComparison.OrdinalIgnoreCase))
            {
                return EventHandlerResult.NoActionNeeded(
                    $"create event for ref {webhookPayload.RefType} is not of interest");
            }

            await this.GitHubClient.Repository.Branch.UpdateBranchProtection(
                webhookPayload.Repository.Id, webhookPayload.Ref,
                getBranchProtectionSettingsUpdate(webhookPayload.Ref, webhookPayload.Repository.DefaultBranch));
            return EventHandlerResult.ActionPerformed("branch protection rule applied");
        }

        private static BranchProtectionSettingsUpdate getBranchProtectionSettingsUpdate(string branchName,
            string repositoryDefaultBranch)
        {
            // For default: prohibits the merge request into default to be merged
            // For other branches: disables force push
            return new BranchProtectionSettingsUpdate(
                requiredStatusChecks: null, // Required. Require status checks to pass before merging. Set to null to disable.
                requiredPullRequestReviews: getBranchProtectionRequiredReviewsUpdate(branchName,
                    repositoryDefaultBranch),
                restrictions: null, // Push access restrictions. Null to disable.
                enforceAdmins: false); // Required. Enforce all configured restrictions for administrators. Set to true to enforce required status checks for repository administrators. Set to null to disable.
        }

        private static BranchProtectionRequiredReviewsUpdate getBranchProtectionRequiredReviewsUpdate(string branchName,
            string repositoryDefaultBranch)
        {
            if (branchName.Equals(repositoryDefaultBranch, StringComparison.OrdinalIgnoreCase))
            {
                return
                    new BranchProtectionRequiredReviewsUpdate(false, false,
                        1); // Prohibits the student from merging the pull request.
            }

            return null;
        }
    }
}
