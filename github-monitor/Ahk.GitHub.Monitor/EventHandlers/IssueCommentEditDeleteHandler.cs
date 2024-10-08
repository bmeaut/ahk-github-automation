using System;
using System.Threading.Tasks;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class IssueCommentEditDeleteHandler(
        Services.IGitHubClientFactory gitHubClientFactory,
        Microsoft.Extensions.Caching.Memory.IMemoryCache cache,
        Microsoft.Extensions.Logging.ILogger logger)
        : RepositoryEventBase<IssueCommentPayload>(gitHubClientFactory, cache, logger)
    {
        public const string GitHubWebhookEventName = "issue_comment";
        private const string WarningText = ":exclamation: **An issue comment was deleted / edited. Egy megjegyzes torolve vagy modositva lett.**";

        protected override async Task<EventHandlerResult> executeCore(IssueCommentPayload webhookPayload)
        {
            if (webhookPayload.Issue == null)
                return EventHandlerResult.PayloadError("no issue information in webhook payload");

            if (webhookPayload.Action.Equals("edited", StringComparison.OrdinalIgnoreCase) || webhookPayload.Action.Equals("deleted", StringComparison.OrdinalIgnoreCase))
            {
                if (webhookPayload.Sender != null && webhookPayload.Comment?.User != null && webhookPayload.Sender.Login == webhookPayload.Comment.User.Login)
                {
                    return EventHandlerResult.NoActionNeeded($"comment action {webhookPayload.Action} by {webhookPayload.Sender.Login} allowed, referencing own comment");
                }

                await this.GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, WarningText);
                return EventHandlerResult.ActionPerformed("comment action resulting in warning");
            }

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }
    }
}
