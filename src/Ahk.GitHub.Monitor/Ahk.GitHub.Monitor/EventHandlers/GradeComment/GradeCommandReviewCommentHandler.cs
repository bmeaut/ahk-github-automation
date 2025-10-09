using Ahk.GitHub.Monitor.EventHandlers.Abstractions;
using Ahk.GitHub.Monitor.EventHandlers.GradeComment.Payload;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;
using Ahk.GitHub.Monitor.Services.GradeStore;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

using System;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers.GradeComment;

public class GradeCommandReviewCommentHandler(
    IGitHubClientFactory gitHubClientFactory,
    IGradeStore gradeStore,
    IMemoryCache cache,
    ILogger<GradeCommandReviewCommentHandler> logger,
    PullRequestStatusTrackingHandler pullRequestStatusTrackingHandler)
    : GradeCommandHandlerBase<PullRequestReviewEventPayload>(gitHubClientFactory, gradeStore, cache, logger, pullRequestStatusTrackingHandler), IGitHubEventHandler
{
    public static string GitHubWebhookEventName => "pull_request_review";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(PullRequestReviewEventPayload webhookPayload)
    {
        if (webhookPayload.Review == null)
        {
            return EventHandlerResult.PayloadError("no review information in webhook payload");
        }

        if (webhookPayload.Action.Equals("submitted", StringComparison.OrdinalIgnoreCase))
        {
            return await ProcessCommentAsync(new ReviewCommentPayloadFacade(webhookPayload));
        }

        return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
    }

    protected override Task HandleReactionAsync(ICommentPayload<PullRequestReviewEventPayload> webhookPayload, ReactionType reactionType)
        => GitHubClient.Reaction.PullRequestReviewComment.Create(
            webhookPayload.Repository.Id,
            webhookPayload.Payload.Review.Id,
            new NewReaction(reactionType));
}
