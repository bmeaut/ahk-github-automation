using Ahk.GitHub.Monitor.EventHandlers.Abstractions;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

using System;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers;

public class IssueCommentEditDeleteHandler(
    IGitHubClientFactory gitHubClientFactory,
    IMemoryCache cache,
    ILogger<IssueCommentEditDeleteHandler> logger)
    : RepositoryEventHandlerBase<IssueCommentPayload>(gitHubClientFactory, cache, logger), IGitHubEventHandler
{
    public static string GitHubWebhookEventName => "issue_comment";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(IssueCommentPayload webhookPayload)
    {
        if (webhookPayload.Issue == null)
        {
            return EventHandlerResult.PayloadError("no issue information in webhook payload");
        }

        if (webhookPayload.Action.Equals("edited", StringComparison.OrdinalIgnoreCase)
            || webhookPayload.Action.Equals("deleted", StringComparison.OrdinalIgnoreCase))
        {
            if (webhookPayload.Sender != null
                && webhookPayload.Comment?.User != null
                && webhookPayload.Sender.Login == webhookPayload.Comment.User.Login)
            {
                return EventHandlerResult.NoActionNeeded($"comment action {webhookPayload.Action} by {webhookPayload.Sender.Login} allowed, referencing own comment");
            }

            await GitHubClient.Issue.Comment.Create(
                webhookPayload.Repository.Id,
                webhookPayload.Issue.Number,
                ":exclamation: **An issue comment was deleted / edited. Egy megjegyzes torolve vagy modositva lett.**");

            return EventHandlerResult.ActionPerformed("comment action resulting in warning");
        }

        return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
    }
}
