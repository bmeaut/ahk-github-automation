using System;
using System.Threading.Tasks;

using Ahk.GitHub.Monitor.EventHandlers.Abstractions;
using Ahk.GitHub.Monitor.EventHandlers.GradeComment.Payload;
using Ahk.GitHub.Monitor.Helpers;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;
using Ahk.GitHub.Monitor.Services.GradeStore;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers.GradeComment;

public abstract class GradeCommandHandlerBase<T>(
    IGitHubClientFactory gitHubClientFactory,
    IGradeStore gradeStore,
    IMemoryCache cache,
    ILogger logger,
    PullRequestStatusTrackingHandler pullRequestStatusTrackingHandler)
    : RepositoryEventHandlerBase<T>(gitHubClientFactory, cache, logger)
    where T : ActivityPayload
{
    private readonly IMemoryCache _isOrgMemberCache = cache;

    protected async Task<EventHandlerResult> ProcessCommentAsync(ICommentPayload<T> webhookPayload)
    {
        var gradeCommand = new GradeCommentParser(webhookPayload.CommentBody);
        if (!gradeCommand.IsMatch)
        {
            return EventHandlerResult.NoActionNeeded("not recognized as command");
        }

        if (!await IsAllowedAsync(webhookPayload))
        {
            return await HandleUserNotAllowedAsync(webhookPayload);
        }

        var pr = await GetPullRequestAsync(webhookPayload);
        if (pr == null)
        {
            return await HandleNotPrAsync(webhookPayload);
        }

        await HandleApproveAsync(webhookPayload, pr);
        await HandleStoreGradeAsync(webhookPayload, gradeCommand, pr);
        await HandleReactionAsync(webhookPayload, ReactionType.Plus1);

        return EventHandlerResult.ActionPerformed($"comment operation to grade done; grades: {string.Join(" ", gradeCommand.GradesWithOrder.Values)}");
    }

    protected abstract Task HandleReactionAsync(ICommentPayload<T> webhookPayload, ReactionType reactionType);

    private async Task<PullRequest> GetPullRequestAsync(ICommentPayload<T> webhookPayload)
    {
        try
        {
            return await GitHubClient.PullRequest.Get(webhookPayload.Repository.Id, webhookPayload.PullRequestNumber);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private async Task HandleStoreGradeAsync(ICommentPayload<T> webhookPayload, GradeCommentParser gradeCommand, PullRequest pr)
    {
        Logger.LogInformation("Storing grades for {RepositoryFullName}", webhookPayload.Repository.FullName);
        if (gradeCommand.HasGrades)
        {
            await gradeStore.StoreGradeAsync(
                webhookPayload.Repository.HtmlUrl,
                pr.HtmlUrl,
                webhookPayload.CommentingUser,
                gradeCommand.GradesWithOrder);
        }
        else
        {
            await gradeStore.ConfirmAutoGradeAsync(
                webhookPayload.Repository.HtmlUrl,
                pr.HtmlUrl,
                webhookPayload.CommentingUser);
        }
    }

    private async Task HandleApproveAsync(ICommentPayload<T> webhookPayload, PullRequest pr)
    {
        var shouldMergePr = pr.State.Value == ItemState.Open && pr.Mergeable == true;
        if (!shouldMergePr)
        {
            Logger.LogInformation("PR is not mergable");
            return;
        }

        Logger.LogInformation("PR ({PrId}) is being merged in {RepositoryUrl}", webhookPayload.PullRequestNumber, webhookPayload.Repository.Url);

        await GitHubClient.PullRequest.Review.Create(
            webhookPayload.Repository.Id,
            webhookPayload.PullRequestNumber,
            new() { Event = PullRequestReviewEvent.Approve });

        await GitHubClient.PullRequest.Merge(
            webhookPayload.Repository.Id,
            webhookPayload.PullRequestNumber,
            new());

        await pullRequestStatusTrackingHandler.PrStatusChangedAsync(
            webhookPayload.Repository.Url,
            webhookPayload.PullRequestUrl,
            PullRequestStatus.Merged);
    }

    private async Task<EventHandlerResult> HandleNotPrAsync(ICommentPayload<T> webhookPayload)
    {
        await HandleReactionAsync(webhookPayload, ReactionType.Confused);
        return EventHandlerResult.ActionPerformed("comment operation to grade not called for PR");
    }

    private async Task<EventHandlerResult> HandleUserNotAllowedAsync(ICommentPayload<T> webhookPayload)
    {
        await HandleReactionAsync(webhookPayload, ReactionType.Confused);

        var comment = $":exclamation: **@{webhookPayload.CommentingUser} is not allowed to do that. @{webhookPayload.CommentingUser} Ez nem engedelyezett szamodra.**";
        await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.PullRequestNumber, comment);

        return EventHandlerResult.ActionPerformed("comment operation to grade not allowed for user");
    }

    private Task<bool> IsAllowedAsync(ICommentPayload<T> webhookPayload)
    {
        if (webhookPayload.Repository.Owner.Type != AccountType.Organization)
        {
            return Task.FromResult(false);
        }

        return _isOrgMemberCache.GetOrCreateAsync(
            $"githubisorgmember{webhookPayload.Repository.Owner.Login}{webhookPayload.CommentingUser}",
            async cacheEntry =>
            {
                var isMember = false;
                try
                {
                    isMember = await GitHubClient.Organization.Member.CheckMember(webhookPayload.Repository.Owner.Login, webhookPayload.CommentingUser);
                }
                catch (NotFoundException)
                {
                }

                cacheEntry.SetValue(isMember);
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(1));

                return isMember;
            });
    }
}
