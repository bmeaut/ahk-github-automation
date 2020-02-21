using Octokit;
using System;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class IssueEventHandler : RepositoryEventBase<IssueEventPayload>
    {
        public const string GitHubWebhookEventName = "issue_comment";
        public const string FeatureFlagName = "AHK_COMMENTEDITWARN_ENABLED";

        public IssueEventHandler(GitHubClient gitHubClient)
            : base(gitHubClient, FeatureFlagName)
        {
        }

        protected override async Task execute(IssueEventPayload webhookPayload, WebhookResult webhookResult)
        {
            if (webhookPayload.Issue == null)
            {
                webhookResult.LogError("no issue information in webhook payload");
            }
            else if (webhookPayload.Action.Equals("edited", StringComparison.OrdinalIgnoreCase) || webhookPayload.Action.Equals("deleted", StringComparison.OrdinalIgnoreCase))
            {
                await gitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, getCommentTextToAdd());
                webhookResult.LogInfo("comment action handled");
            }
            else
            {
                webhookResult.LogInfo($"comment action {webhookPayload.Action} is not of interrest");
            }
        }

        private static string getCommentTextToAdd()
        {
            var commentMsg = Environment.GetEnvironmentVariable("AHK_COMMENTEDITWARN_MESSAGE", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(commentMsg) || string.IsNullOrWhiteSpace(commentMsg))
                return @":exclamation: **An issue comment was deleted / edited. Egy megjegyzes torolve vagy modositva lett.** \n\n _This is an automated message. Ez egy automata uzenet._";
            else
                return commentMsg;
        }
    }
}
