using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class IssueCommentCommandHandler : RepositoryEventBase<IssueCommentPayload>
    {
        public const string GitHubWebhookEventName = "issue_comment";
        private const string WarningTextPRInvalidState = ":confused: **PR cannot be merged or does not exist. A PR nem letezik vagy nem merge-elheto.**";
        private const string WarningTextUserNotAllowed = ":confused: **Accepting not allowed for {}.**";

        private static readonly Regex CommandRegex = new Regex(@"(^|\r|\n|\r\n)@ahk ok($|\r|\n|\r\n)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        public IssueCommentCommandHandler(Services.IGitHubClientFactory gitHubClientFactory)
            : base(gitHubClientFactory)
        {
        }

        protected override async Task<EventHandlerResult> execute(IssueCommentPayload webhookPayload)
        {
            if (webhookPayload.Issue == null)
                return EventHandlerResult.PayloadError("no issue information in webhook payload");

            if (webhookPayload.Action.Equals("created", StringComparison.OrdinalIgnoreCase))
            {
                if (isOkCommand(webhookPayload.Comment))
                {
                    if (!await isAllowed(webhookPayload))
                        return await handleUserNotAllowed(webhookPayload);

                    var pr = await tryGetPullRequest(webhookPayload.Repository.Id, webhookPayload.Issue.Number);
                    if (pr == null || pr.State.Value != ItemState.Open || pr.Mergeable != true)
                        return await handleNotValidPRState(webhookPayload);

                    return await handleApprove(webhookPayload);
                }
                else
                {
                    return EventHandlerResult.NoActionNeeded("not recognized as command");
                }
            }

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
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

        private async Task<EventHandlerResult> handleApprove(IssueCommentPayload webhookPayload)
        {
            await GitHubClient.Reaction.IssueComment.Create(webhookPayload.Repository.Id, webhookPayload.Comment.Id, new NewReaction(ReactionType.Plus1));

            await GitHubClient.PullRequest.Review.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, new PullRequestReviewCreate() { Event = PullRequestReviewEvent.Approve });
            await GitHubClient.PullRequest.Merge(webhookPayload.Repository.Id, webhookPayload.Issue.Number, new MergePullRequest());
            return EventHandlerResult.ActionPerformed("comment operation to accept done");
        }

        private async Task<EventHandlerResult> handleNotValidPRState(IssueCommentPayload webhookPayload)
        {
            await GitHubClient.Reaction.IssueComment.Create(webhookPayload.Repository.Id, webhookPayload.Comment.Id, new NewReaction(ReactionType.Confused));
            await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, WarningTextPRInvalidState);
            return EventHandlerResult.ActionPerformed("comment operation to accept not valid due to PR state");
        }

        private async Task<EventHandlerResult> handleUserNotAllowed(IssueCommentPayload webhookPayload)
        {
            await GitHubClient.Reaction.IssueComment.Create(webhookPayload.Repository.Id, webhookPayload.Comment.Id, new NewReaction(ReactionType.Confused));
            await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, WarningTextUserNotAllowed.Replace("{}", webhookPayload.Comment.User.Login));
            return EventHandlerResult.ActionPerformed("comment operation to accept not allowed for user");
        }

        private async Task<bool> isAllowed(IssueCommentPayload webhookPayload)
        {
            if (webhookPayload.Repository.Owner.Type != AccountType.Organization)
                return false;

            try
            {
                return await GitHubClient.Organization.Member.CheckMember(webhookPayload.Repository.Owner.Login, webhookPayload.Comment.User.Login);
            }
            catch (NotFoundException)
            {
                return false;
            }
        }

        private bool isOkCommand(IssueComment comment) => CommandRegex.IsMatch(comment.Body);
    }
}
