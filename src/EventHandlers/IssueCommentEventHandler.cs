using System;
using System.Threading.Tasks;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class IssueCommentEventHandler : RepositoryEventBase<IssueCommentPayload>
    {
        public const string GitHubWebhookEventName = "issue_comment";
        private const string DefaultWarningText = ":exclamation: **An issue comment was deleted / edited. Egy megjegyzes torolve vagy modositva lett.** \n\n _This is an automated message. Ez egy automata uzenet._";

        public IssueCommentEventHandler(Services.IGitHubClientFactory gitHubClientFactory)
            : base(gitHubClientFactory)
        {
        }

        protected override async Task execute(GitHubClient gitHubClient, IssueCommentPayload webhookPayload, RepositorySettings repoSettings, WebhookResult webhookResult)
        {
            if (webhookPayload.Issue == null)
            {
                webhookResult.LogError("no issue information in webhook payload");
            }
            else if (repoSettings.CommentProtection == null || !repoSettings.CommentProtection.Enabled)
            {
                webhookResult.LogError("comment protection not enabled for repository");
            }
            else if (webhookPayload.Action.Equals("edited", StringComparison.OrdinalIgnoreCase) || webhookPayload.Action.Equals("deleted", StringComparison.OrdinalIgnoreCase))
            {
                if (webhookPayload.Sender != null && webhookPayload.Comment?.User != null && webhookPayload.Sender.Login == webhookPayload.Comment.User.Login)
                {
                    webhookResult.LogInfo($"comment action {webhookPayload.Action} by {webhookPayload.Sender.Login} allowed, referencing own comment");
                }
                else
                {
                    await gitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, getWarningText(repoSettings.CommentProtection));
                    webhookResult.LogInfo("comment action handled");
                }
            }
            else
            {
                webhookResult.LogInfo($"comment action {webhookPayload.Action} is not of interrest");
            }
        }

        private static string getWarningText(CommentProtectionSettings commentProtection)
            => string.IsNullOrEmpty(commentProtection.WarningText) ? DefaultWarningText : commentProtection.WarningText;
    }
}
