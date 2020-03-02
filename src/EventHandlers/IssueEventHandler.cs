using Octokit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class IssueEventHandler : RepositoryEventBase<IssueCommentPayload>
    {
        public const string GitHubWebhookEventName = "issue_comment";
        public const string FeatureFlagName = "AHK_COMMENTEDITWARN_ENABLED";

        public IssueEventHandler(GitHubClient gitHubClient)
            : base(gitHubClient, FeatureFlagName)
        {
        }

        protected override async Task execute(IssueCommentPayload webhookPayload, WebhookResult webhookResult)
        {
            if (webhookPayload.Issue == null)
            {
                webhookResult.LogError("no issue information in webhook payload");
            }
            else if (webhookPayload.Action.Equals("edited", StringComparison.OrdinalIgnoreCase) || webhookPayload.Action.Equals("deleted", StringComparison.OrdinalIgnoreCase))
            {
                if (webhookPayload.Sender != null && isUserAllowedToEdit(webhookPayload.Sender.Login))
                {
                    webhookResult.LogInfo($"comment action {webhookPayload.Action} is allowed for user {webhookPayload.Sender.Login}");
                }
                else
                {
                    await gitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, getWarningTextToAdd());
                    webhookResult.LogInfo("comment action handled");
                }
            }
            else
            {
                webhookResult.LogInfo($"comment action {webhookPayload.Action} is not of interrest");
            }
        }

        private static string getWarningTextToAdd()
        {
            var commentMsg = Environment.GetEnvironmentVariable("AHK_COMMENTEDITWARN_MESSAGE", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(commentMsg) || string.IsNullOrWhiteSpace(commentMsg))
                return @":exclamation: **An issue comment was deleted / edited. Egy megjegyzes torolve vagy modositva lett.** \n\n _This is an automated message. Ez egy automata uzenet._";
            else
                return commentMsg;
        }

        private static bool isUserAllowedToEdit(string username)
        {
            var allowedUsernames = Environment.GetEnvironmentVariable("AHK_COMMENTEDITWARN_ALLOWEDUSERS", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(allowedUsernames))
                return false;
            return allowedUsernames.Split(';').Any(allowedName => username.Equals(allowedName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
