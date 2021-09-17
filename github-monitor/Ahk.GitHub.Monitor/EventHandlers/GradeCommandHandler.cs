using Ahk.GitHub.Monitor.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Octokit;
using System;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class GradeCommandHandler : RepositoryEventBase<IssueCommentPayload>
    {
        public const string GitHubWebhookEventName = "issue_comment";

        private const string WarningText = ":exclamation: **@{} is not allowed to do that. @{} Ez nem engedelyezett szamodra.**";

        private readonly Services.IGradeStore gradeStore;
        private readonly IMemoryCache isOrgMemberCache;

        public GradeCommandHandler(Services.IGitHubClientFactory gitHubClientFactory, Services.IGradeStore gradeStore, IMemoryCache cache)
            : base(gitHubClientFactory, cache)
        {
            this.gradeStore = gradeStore;
            this.isOrgMemberCache = cache;
        }

        protected override async Task<EventHandlerResult> executeCore(IssueCommentPayload webhookPayload)
        {
            if (webhookPayload.Issue == null)
                return EventHandlerResult.PayloadError("no issue information in webhook payload");

            if (webhookPayload.Action.Equals("created", StringComparison.OrdinalIgnoreCase))
            {
                var gradeCommand = new GradeCommentParser(webhookPayload.Comment.Body);
                if (gradeCommand.IsMatch)
                {
                    if (!await isAllowed(webhookPayload))
                        return await handleUserNotAllowed(webhookPayload);

                    var pr = await tryGetPullRequest(webhookPayload.Repository.Id, webhookPayload.Issue.Number);
                    if (pr == null)
                        return await handleNotPr(webhookPayload);

                    await handleApprove(webhookPayload, pr);
                    await handleStoreGrade(webhookPayload, gradeCommand, pr);

                    await GitHubClient.Reaction.IssueComment.Create(webhookPayload.Repository.Id, webhookPayload.Comment.Id, new NewReaction(ReactionType.Plus1));
                    return EventHandlerResult.ActionPerformed($"comment operation to grade done; grades: {string.Join(" ", gradeCommand.Grades)}");
                }
                else
                {
                    return EventHandlerResult.NoActionNeeded("not recognized as command");
                }
            }

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }

        private async Task handleStoreGrade(IssueCommentPayload webhookPayload, GradeCommentParser gradeCommand, PullRequest pr)
        {
            if (gradeCommand.HasGrades)
            {
                var neptun = await getNeptun(webhookPayload, pr.Head.Ref);
                await gradeStore.StoreGrade(
                    neptun: neptun,
                    repository: webhookPayload.Repository.FullName,
                    prNumber: pr.Number,
                    prUrl: pr.HtmlUrl,
                    actor: webhookPayload.Comment.User?.Login,
                    origin: webhookPayload.Comment.HtmlUrl,
                    results: gradeCommand.Grades);
            }
        }

        private async Task<PullRequest> tryGetPullRequest(long id, int number)
        {
            try
            {
                return await GitHubClient.PullRequest.Get(id, number);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }

        private async Task handleApprove(IssueCommentPayload webhookPayload, PullRequest pr)
        {
            bool shouldMergePr = pr.State.Value == ItemState.Open && pr.Mergeable == true;
            if (shouldMergePr)
            {
                await GitHubClient.PullRequest.Review.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, new PullRequestReviewCreate() { Event = PullRequestReviewEvent.Approve });
                await GitHubClient.PullRequest.Merge(webhookPayload.Repository.Id, webhookPayload.Issue.Number, new MergePullRequest());
            }
        }

        private async Task<EventHandlerResult> handleNotPr(IssueCommentPayload webhookPayload)
        {
            await GitHubClient.Reaction.IssueComment.Create(webhookPayload.Repository.Id, webhookPayload.Comment.Id, new NewReaction(ReactionType.Confused));
            return EventHandlerResult.ActionPerformed("comment operation to grade not called for PR");
        }

        private async Task<EventHandlerResult> handleUserNotAllowed(IssueCommentPayload webhookPayload)
        {
            await GitHubClient.Reaction.IssueComment.Create(webhookPayload.Repository.Id, webhookPayload.Comment.Id, new NewReaction(ReactionType.Confused));

            var comment = WarningText.Replace("{}", webhookPayload.Comment.User.Login, StringComparison.OrdinalIgnoreCase);
            await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, comment);

            return EventHandlerResult.ActionPerformed("comment operation to grade not allowed for user");
        }

        private Task<bool> isAllowed(IssueCommentPayload webhookPayload)
        {
            if (webhookPayload.Repository.Owner.Type != AccountType.Organization)
                return Task.FromResult(false);

            return isOrgMemberCache.GetOrCreateAsync(
                key: $"githubisorgmember{webhookPayload.Repository.Owner.Login}{webhookPayload.Comment.User.Login}",
                factory: async cacheEntry =>
                {
                    var isMember = await getIsUserOrgMember(webhookPayload.Repository.Owner.Login, webhookPayload.Comment.User.Login);
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
