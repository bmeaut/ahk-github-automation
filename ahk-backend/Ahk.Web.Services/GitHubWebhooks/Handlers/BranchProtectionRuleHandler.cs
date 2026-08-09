using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers;

/// <summary>
/// Applies the course's branch rules when a branch is created: the default branch requires a review (so the
/// student cannot merge their own pull request), every other branch is left alone beyond disabling force push.
/// Ported from <c>github-monitor/.../EventHandlers/BranchProtectionRuleHandler.cs</c>.
/// </summary>
public sealed class BranchProtectionRuleHandler : RepositoryEventHandlerBase<CreateEventPayload>
{
    public BranchProtectionRuleHandler(IMemoryCache cache, ILogger<BranchProtectionRuleHandler> logger)
        : base(cache, logger)
    {
    }

    public override string GitHubEventName => "create";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(GitHubWebhookContext context, CreateEventPayload payload, CancellationToken cancellationToken)
    {
        // StringValue rather than the RefType enum: Octokit throws on ref types it does not know, and a new
        // one appearing must not take the handler down.
        if (!payload.RefType.StringValue.Equals("branch", StringComparison.OrdinalIgnoreCase))
            return EventHandlerResult.NoActionNeeded($"create event for ref {payload.RefType} is not of interest");

        await context.GitHubClient.Repository.Branch.UpdateBranchProtection(
            payload.Repository.Id, payload.Ref, GetBranchProtectionSettingsUpdate(payload.Ref, payload.Repository.DefaultBranch));

        return EventHandlerResult.ActionPerformed("branch protection rule applied");
    }

    private static BranchProtectionSettingsUpdate GetBranchProtectionSettingsUpdate(string branchName, string repositoryDefaultBranch)
    {
        // For default: prohibits the merge request into default to be merged.
        // For other branches: disables force push.
        return new BranchProtectionSettingsUpdate(
            requiredStatusChecks: null,
            requiredPullRequestReviews: GetBranchProtectionRequiredReviewsUpdate(branchName, repositoryDefaultBranch),
            restrictions: null,
            enforceAdmins: false);
    }

    private static BranchProtectionRequiredReviewsUpdate? GetBranchProtectionRequiredReviewsUpdate(string branchName, string repositoryDefaultBranch)
        => branchName.Equals(repositoryDefaultBranch, StringComparison.OrdinalIgnoreCase)
            ? new BranchProtectionRequiredReviewsUpdate(false, false, 1) // Prohibits the student from merging the pull request.
            : null;
}
