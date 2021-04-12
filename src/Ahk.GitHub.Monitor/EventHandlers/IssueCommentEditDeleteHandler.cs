using System;
using System.Threading.Tasks;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class IssueCommentEditDeleteHandler : RepositoryEventBase<IssueCommentPayload>
    {
        public const string GitHubWebhookEventName = "issue_comment";
        private const string DefaultWarningText = ":exclamation: **An issue comment was deleted / edited. Egy megjegyzes torolve vagy modositva lett.** \n\n _This is an automated message. Ez egy automata uzenet._";

        public IssueCommentEditDeleteHandler(Services.IGitHubClientFactory gitHubClientFactory)
            : base(gitHubClientFactory)
        {
        }

        protected override async Task<EventHandlerResult> execute(IssueCommentPayload webhookPayload, RepositorySettings repoSettings)
        {
            if (webhookPayload.Issue == null)
                return EventHandlerResult.PayloadError("no issue information in webhook payload");

            if (repoSettings.CommentProtection == null || !repoSettings.CommentProtection.Enabled)
                return EventHandlerResult.Disabled();

            if (webhookPayload.Action.Equals("edited", StringComparison.OrdinalIgnoreCase) || webhookPayload.Action.Equals("deleted", StringComparison.OrdinalIgnoreCase))
            {
                if (webhookPayload.Sender != null && webhookPayload.Comment?.User != null && webhookPayload.Sender.Login == webhookPayload.Comment.User.Login)
                {
                    return EventHandlerResult.NoActionNeeded($"comment action {webhookPayload.Action} by {webhookPayload.Sender.Login} allowed, referencing own comment");
                }
                else
                {
                    await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, getWarningText(repoSettings.CommentProtection));
                    return EventHandlerResult.ActionPerformed("comment action resulting in warning");
                }
            }

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }

        private static string getWarningText(CommentProtectionSettings commentProtection)
            => string.IsNullOrEmpty(commentProtection.WarningText) ? DefaultWarningText : commentProtection.WarningText;
    }
}
