using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Helpers;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class IssueCommentCommandHandler : RepositoryEventBase<IssueCommentPayload>
    {
        public const string GitHubWebhookEventName = "issue_comment";
        private const string WarningTextUserNotAllowed = ":confused: **Grading not allowed for {}.**";


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
                var gradeCommand = new GradeCommentParser(webhookPayload.Comment.Body);
                if (gradeCommand.IsMatch)
                {
                    if (!await isAllowed(webhookPayload))
                        return await handleUserNotAllowed(webhookPayload);

                    var pr = await tryGetPullRequest(webhookPayload.Repository.Id, webhookPayload.Issue.Number);
                    if (pr == null)
                        return await handleNotPr(webhookPayload);

                    return await handleApprove(webhookPayload, pr, gradeCommand);
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

        private async Task<EventHandlerResult> handleApprove(IssueCommentPayload webhookPayload, PullRequest pr, GradeCommentParser grades)
        {
            await GitHubClient.Reaction.IssueComment.Create(webhookPayload.Repository.Id, webhookPayload.Comment.Id, new NewReaction(ReactionType.Plus1));

            bool shouldMergePr = pr.State.Value == ItemState.Open && pr.Mergeable == true;
            if (shouldMergePr)
            {
                await GitHubClient.PullRequest.Review.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, new PullRequestReviewCreate() { Event = PullRequestReviewEvent.Approve });
                await GitHubClient.PullRequest.Merge(webhookPayload.Repository.Id, webhookPayload.Issue.Number, new MergePullRequest());
            }

            return EventHandlerResult.ActionPerformed($"comment operation to grade done; grades: {string.Join(" ", grades.Grades)}");
        }

        private async Task<EventHandlerResult> handleNotPr(IssueCommentPayload webhookPayload)
        {
            await GitHubClient.Reaction.IssueComment.Create(webhookPayload.Repository.Id, webhookPayload.Comment.Id, new NewReaction(ReactionType.Confused));
            return EventHandlerResult.ActionPerformed("comment operation to grade not called for PR");
        }

        private async Task<EventHandlerResult> handleUserNotAllowed(IssueCommentPayload webhookPayload)
        {
            await GitHubClient.Reaction.IssueComment.Create(webhookPayload.Repository.Id, webhookPayload.Comment.Id, new NewReaction(ReactionType.Confused));
            await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, WarningTextUserNotAllowed.Replace("{}", webhookPayload.Comment.User.Login));
            return EventHandlerResult.ActionPerformed("comment operation to grade not allowed for user");
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
    }
}
