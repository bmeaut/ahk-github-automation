using Ahk.GitHub.Monitor.EventHandlers.Abstractions;
using Ahk.GitHub.Monitor.EventHandlers.GradeComment.Payload;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;

using MassTransit;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

using System;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers.GradeComment;

public class GradeCommandIssueCommentHandler(
    IGitHubClientFactory gitHubClientFactory,
    IPublishEndpoint publishEndpoint,
    IMemoryCache cache,
    ILogger<GradeCommandIssueCommentHandler> logger,
    PullRequestStatusTrackingHandler pullRequestStatusTrackingHandler)
    : GradeCommandHandlerBase<IssueCommentPayload>(gitHubClientFactory, publishEndpoint, cache, logger, pullRequestStatusTrackingHandler), IGitHubEventHandler
{
    public static string GitHubWebhookEventName => "issue_comment";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(IssueCommentPayload webhookPayload)
    {
        if (webhookPayload.Issue == null)
        {
            return EventHandlerResult.PayloadError("no issue information in webhook payload");
        }

        if (webhookPayload.Action.Equals("created", StringComparison.OrdinalIgnoreCase))
        {
            return await ProcessCommentAsync(new IssueCommentPayloadFacade(webhookPayload));
        }

        return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
    }

    protected override Task HandleReactionAsync(ICommentPayload<IssueCommentPayload> webhookPayload, ReactionType reactionType)
        => GitHubClient.Reaction.IssueComment.Create(
            webhookPayload.Repository.Id,
            webhookPayload.Payload.Comment.Id,
            new NewReaction(reactionType));
}
