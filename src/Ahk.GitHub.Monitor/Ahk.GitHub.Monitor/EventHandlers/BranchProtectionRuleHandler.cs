using System;
using System.Threading.Tasks;

using Ahk.GitHub.Monitor.EventHandlers.Abstractions;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers;

public class BranchProtectionRuleHandler(
    IGitHubClientFactory gitHubClientFactory,
    IMemoryCache cache,
    ILogger<BranchProtectionRuleHandler> logger)
    : RepositoryEventHandlerBase<CreateEventPayload>(gitHubClientFactory, cache, logger), IGitHubEventHandler
{
    public static string GitHubWebhookEventName => "create";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(CreateEventPayload webhookPayload)
    {
        if (!webhookPayload.RefType.StringValue.Equals("branch", StringComparison.OrdinalIgnoreCase))
        {
            return EventHandlerResult.NoActionNeeded($"create event for ref {webhookPayload.RefType} is not of interest");
        }

        // For default: prohibits the merge request into default to be merged
        // For other branches: disables force push
        await GitHubClient.Repository.Branch.UpdateBranchProtection(
            webhookPayload.Repository.Id,
            webhookPayload.Ref,
            new BranchProtectionSettingsUpdate(
                // Required. Require status checks to pass before merging. Set to null to disable.
                requiredStatusChecks: null,
                // Prohibits the student from merging the pull request.
                requiredPullRequestReviews: webhookPayload.Ref.Equals(webhookPayload.Repository.DefaultBranch, StringComparison.OrdinalIgnoreCase)
                    ? new BranchProtectionRequiredReviewsUpdate(false, false, 1)
                    : null,
                // Push access restrictions. Null to disable.
                restrictions: null,
                // Required. Enforce all configured restrictions for administrators.
                // Set to true to enforce required status checks for repository administrators. Set to false to disable.
                enforceAdmins: false));

        return EventHandlerResult.ActionPerformed("branch protection rule applied");
    }
}
