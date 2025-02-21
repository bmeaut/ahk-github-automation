using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers.BaseAndUtils;
using Ahk.GitHub.Monitor.EventHandlers.GradeComment.Payload;
using Ahk.GitHub.Monitor.Services;
using Ahk.GitHub.Monitor.Services.GradeStore;
using Microsoft.Extensions.Caching.Memory;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers.GradeComment;

public class GradeCommandReviewCommentHandler(
    IGitHubClientFactory gitHubClientFactory,
    IGradeStore gradeStore,
    IMemoryCache cache,
    IServiceProvider serviceProvider,
    PullRequestStatusTrackingHandler pullRequestStatusTrackingHandler)
    : GradeCommandHandlerBase<PullRequestReviewEventPayload>(gitHubClientFactory, gradeStore, cache,
        serviceProvider, pullRequestStatusTrackingHandler)
{
    public const string GitHubWebhookEventName = "pull_request_review";

    protected override async Task<EventHandlerResult> executeCore(PullRequestReviewEventPayload webhookPayload)
    {
        if (webhookPayload.Review == null)
        {
            return EventHandlerResult.PayloadError("no review information in webhook payload");
        }

        if (webhookPayload.Action.Equals("submitted", StringComparison.OrdinalIgnoreCase))
        {
            return await this.processComment(new ReviewCommentPayloadFacade(webhookPayload));
        }

        return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
    }

    protected override Task handleReaction(
        ICommentPayload<PullRequestReviewEventPayload> webhookPayload, ReactionType reactionType) =>
        // The implementation below does not work, GitHub API returns an error indicating that the resource is not available for the GitHub App
        // According to documentation "pull request" read/write permission should be enough to add the reaction, but it seems insufficient.
        Task.CompletedTask;
    /*
        // discrepancy in the Octokit library regarding long-int ids
        if (webhookPayload.Payload.Review.Id > int.MaxValue)
            return Task.CompletedTask;

        return GitHubClient.Reaction.PullRequestReviewComment.Create(webhookPayload.Repository.Id, (int)webhookPayload.Payload.Review.Id, new NewReaction(reactionType));
        */
}
