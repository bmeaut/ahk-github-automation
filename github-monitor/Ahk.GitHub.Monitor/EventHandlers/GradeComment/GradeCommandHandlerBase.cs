using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers.GradeComment.Payload;
using Ahk.GitHub.Monitor.EventHandlers.StatusTracking;
using Ahk.GitHub.Monitor.Helpers;
using Ahk.GitHub.Monitor.Services;
using Ahk.GitHub.Monitor.Services.GradeStore;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers.GradeComment
{
    public abstract class GradeCommandHandlerBase<T>(
        IGitHubClientFactory gitHubClientFactory,
        IGradeStore gradeStore,
        IMemoryCache cache,
        IServiceProvider serviceProvider,
        PullRequestStatusTrackingHandler pullRequestStatusTrackingHandler)
        : RepositoryEventBase<T>(gitHubClientFactory, cache, serviceProvider)
        where T : ActivityPayload
    {
        private const string WarningText =
            ":exclamation: **@{} is not allowed to do that. @{} Ez nem engedelyezett szamodra.**";

        private readonly IMemoryCache isOrgMemberCache = cache;

        protected async Task<EventHandlerResult> processComment(ICommentPayload<T> webhookPayload)
        {
            var gradeCommand = new GradeCommentParser(webhookPayload.CommentBody);
            if (gradeCommand.IsMatch)
            {
                if (!await this.isAllowed(webhookPayload))
                    return await this.handleUserNotAllowed(webhookPayload);

                var pr = await this.getPullRequest(webhookPayload);
                if (pr == null)
                    return await this.handleNotPr(webhookPayload);

                await this.handleApprove(webhookPayload, pr);
                await this.handleStoreGrade(webhookPayload, gradeCommand, pr);

                await this.handleReaction(webhookPayload, ReactionType.Plus1);
                return EventHandlerResult.ActionPerformed(
                    $"comment operation to grade done; grades: {string.Join(" ", gradeCommand.GradesWithOrder.Values)}");
            }

            return EventHandlerResult.NoActionNeeded("not recognized as command");
        }

        protected abstract Task handleReaction(ICommentPayload<T> webhookPayload, ReactionType reactionType);

        private async Task<PullRequest> getPullRequest(ICommentPayload<T> webhookPayload)
        {
            try
            {
                return await this.GitHubClient.PullRequest.Get(webhookPayload.Repository.Id,
                    webhookPayload.PullRequestNumber);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }

        private async Task handleStoreGrade(ICommentPayload<T> webhookPayload, GradeCommentParser gradeCommand,
            PullRequest pr)
        {
            Logger.LogInformation($"storing grades for {webhookPayload.Repository.FullName}");
            if (gradeCommand.HasGrades)
            {
                await gradeStore.StoreGrade(
                    repositoryUrl: webhookPayload.Repository.FullName,
                    prUrl: pr.HtmlUrl,
                    actor: webhookPayload.CommentingUser,
                    results: gradeCommand.GradesWithOrder);
            }
            else
            {
                await gradeStore.ConfirmAutoGrade(
                    repositoryUrl: webhookPayload.Repository.Url,
                    prUrl: pr.HtmlUrl,
                    actor: webhookPayload.CommentingUser);
            }
        }

        private async Task handleApprove(ICommentPayload<T> webhookPayload, PullRequest pr)
        {
            bool shouldMergePr = pr.State.Value == ItemState.Open && pr.Mergeable == true;
            if (shouldMergePr)
            {
                Logger.LogInformation("PR is being merged");
                await this.GitHubClient.PullRequest.Review.Create(webhookPayload.Repository.Id,
                    webhookPayload.PullRequestNumber,
                    new PullRequestReviewCreate() { Event = PullRequestReviewEvent.Approve });
                await this.GitHubClient.PullRequest.Merge(webhookPayload.Repository.Id,
                    webhookPayload.PullRequestNumber,
                    new MergePullRequest());
                await pullRequestStatusTrackingHandler.PrStatusChanged(webhookPayload.Repository.Url,
                    webhookPayload.PullRequestUrl, PullRequestStatus.Merged);
            }
            else
            {
                Logger.LogInformation("PR is not mergable");
            }
        }

        private async Task<EventHandlerResult> handleNotPr(ICommentPayload<T> webhookPayload)
        {
            await this.handleReaction(webhookPayload, ReactionType.Confused);
            return EventHandlerResult.ActionPerformed("comment operation to grade not called for PR");
        }

        private async Task<EventHandlerResult> handleUserNotAllowed(ICommentPayload<T> webhookPayload)
        {
            await this.handleReaction(webhookPayload, ReactionType.Confused);

            var comment = WarningText.Replace("{}", webhookPayload.CommentingUser, StringComparison.OrdinalIgnoreCase);
            await this.GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.PullRequestNumber,
                comment);

            return EventHandlerResult.ActionPerformed("comment operation to grade not allowed for user");
        }

        private Task<bool> isAllowed(ICommentPayload<T> webhookPayload)
        {
            if (webhookPayload.Repository.Owner.Type != AccountType.Organization)
                return Task.FromResult(false);

            return isOrgMemberCache.GetOrCreateAsync(
                key: $"githubisorgmember{webhookPayload.Repository.Owner.Login}{webhookPayload.CommentingUser}",
                factory: async cacheEntry =>
                {
                    var isMember = await this.getIsUserOrgMember(webhookPayload.Repository.Owner.Login,
                        webhookPayload.CommentingUser);
                    cacheEntry.SetValue(isMember);
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                    return isMember;
                });
        }

        private async Task<bool> getIsUserOrgMember(string org, string user)
        {
            try
            {
                return await this.GitHubClient.Organization.Member.CheckMember(org, user);
            }
            catch (NotFoundException)
            {
                return false;
            }
        }
    }
}
