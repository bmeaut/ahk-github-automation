using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class IssueCommentEventHandler : RepositoryEventBase<IssueCommentPayload>
    {
        public const string GitHubWebhookEventName = "issue_comment";
        private const string WarningText = ":exclamation: **An issue comment was deleted / edited. Egy megjegyzes torolve vagy modositva lett.** \n\n _This is an automated message. Ez egy automata uzenet._";

        public IssueCommentEventHandler(IOptions<GitHubMonitorConfig> config, Services.IGitHubClientFactory gitHubClientFactory)
            : base(config, gitHubClientFactory)
        {
        }

        protected override async Task execute(GitHubClient gitHubClient, IssueCommentPayload webhookPayload, WebhookResult webhookResult)
        {
            if (webhookPayload.Issue == null)
            {
                webhookResult.LogError("no issue information in webhook payload");
            }
            else if (webhookPayload.Action.Equals("edited", StringComparison.OrdinalIgnoreCase) || webhookPayload.Action.Equals("deleted", StringComparison.OrdinalIgnoreCase))
            {
                if (webhookPayload.Sender != null && webhookPayload.Comment?.User != null && webhookPayload.Sender.Login == webhookPayload.Comment.User.Login)
                {
                    webhookResult.LogInfo($"comment action {webhookPayload.Action} by {webhookPayload.Sender.Login} allowed, referencing own comment");
                }
                else
                {
                    await gitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, WarningText);
                    webhookResult.LogInfo("comment action handled");
                }
            }
            else
            {
                webhookResult.LogInfo($"comment action {webhookPayload.Action} is not of interrest");
            }
        }

    }
}
