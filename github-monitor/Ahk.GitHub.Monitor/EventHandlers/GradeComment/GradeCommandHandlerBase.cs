using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public abstract class GradeCommandHandlerBase<T> : RepositoryEventBase<T>
        where T : ActivityPayload
    {
        private const string WarningText = ":exclamation: **@{} is not allowed to do that. @{} Ez nem engedelyezett szamodra.**";

        private readonly Services.IGradeStore gradeStore;
        private readonly IMemoryCache isOrgMemberCache;

        protected GradeCommandHandlerBase(Services.IGitHubClientFactory gitHubClientFactory, Services.IGradeStore gradeStore, IMemoryCache cache, Microsoft.Extensions.Logging.ILogger logger)
            : base(gitHubClientFactory, cache, logger)
        {
            this.gradeStore = gradeStore;
            this.isOrgMemberCache = cache;
        }

        protected async Task<EventHandlerResult> processComment(ICommentPayload<T> webhookPayload)
        {
            var gradeCommand = new GradeCommentParser(webhookPayload.CommentBody);
            if (gradeCommand.IsMatch)
            {
                if (!await isAllowed(webhookPayload))
                    return await handleUserNotAllowed(webhookPayload);

                var pr = await getPullRequest(webhookPayload);
                if (pr == null)
                    return await handleNotPr(webhookPayload);

                await handleApprove(webhookPayload, pr);
                await handleStoreGrade(webhookPayload, gradeCommand, pr);

                await handleReaction(webhookPayload, ReactionType.Plus1);
                return EventHandlerResult.ActionPerformed($"comment operation to grade done; grades: {string.Join(" ", gradeCommand.Grades)}");
            }
            else
            {
                return EventHandlerResult.NoActionNeeded("not recognized as command");
            }
        }

        protected abstract Task handleReaction(ICommentPayload<T> webhookPayload, ReactionType reactionType);

        protected async Task<PullRequest> getPullRequest(ICommentPayload<T> webhookPayload)
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

        private async Task handleStoreGrade(ICommentPayload<T> webhookPayload, GradeCommentParser gradeCommand, PullRequest pr)
        {
            var neptun = await getNeptun(webhookPayload.Repository.Id, pr.Head.Ref);
            Logger.LogInformation($"storing grades for {neptun}");
            if (gradeCommand.HasGrades)
            {
                await gradeStore.StoreGrade(
                    neptun: neptun,
                    repository: webhookPayload.Repository.FullName,
                    prNumber: pr.Number,
                    prUrl: pr.HtmlUrl,
                    actor: webhookPayload.CommentingUser,
                    origin: webhookPayload.CommentHtmlUrl,
                    results: gradeCommand.Grades);
            }
            else
            {
                await gradeStore.ConfirmAutoGrade(
                    neptun: neptun,
                    repository: webhookPayload.Repository.FullName,
                    prNumber: pr.Number,
                    prUrl: pr.HtmlUrl,
                    actor: webhookPayload.CommentingUser,
                    origin: webhookPayload.CommentHtmlUrl);
            }
        }

        private async Task handleApprove(ICommentPayload<T> webhookPayload, PullRequest pr)
        {
            bool shouldMergePr = pr.State.Value == ItemState.Open && pr.Mergeable == true;
            if (shouldMergePr)
            {
                Logger.LogInformation("PR is being merged");
                await GitHubClient.PullRequest.Review.Create(webhookPayload.Repository.Id, webhookPayload.PullRequestNumber, new PullRequestReviewCreate() { Event = PullRequestReviewEvent.Approve });
                await GitHubClient.PullRequest.Merge(webhookPayload.Repository.Id, webhookPayload.PullRequestNumber, new MergePullRequest());
            }
            else
            {
                Logger.LogInformation("PR is not mergable");
            }
        }

        private async Task<EventHandlerResult> handleNotPr(ICommentPayload<T> webhookPayload)
        {
            await handleReaction(webhookPayload, ReactionType.Confused);
            return EventHandlerResult.ActionPerformed("comment operation to grade not called for PR");
        }

        private async Task<EventHandlerResult> handleUserNotAllowed(ICommentPayload<T> webhookPayload)
        {
            await handleReaction(webhookPayload, ReactionType.Confused);

            var comment = WarningText.Replace("{}", webhookPayload.CommentingUser, StringComparison.OrdinalIgnoreCase);
            await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.PullRequestNumber, comment);

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
                    var isMember = await getIsUserOrgMember(webhookPayload.Repository.Owner.Login, webhookPayload.CommentingUser);
                    cacheEntry.SetValue(isMember);
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                    return isMember;
                });
        }

        private async Task<bool> getIsUserOrgMember(string org, string user)
        {
            try
            {
                return await GitHubClient.Organization.Member.CheckMember(org, user);
            }
            catch (NotFoundException)
            {
                return false;
            }
        }
    }
}
