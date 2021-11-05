using Microsoft.Extensions.Caching.Memory;
using Octokit;
using System;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class GradeCommandReviewCommentHandler : GradeCommandHandlerBase<PullRequestReviewEventPayload>
    {
        public const string GitHubWebhookEventName = "pull_request_review";

        public GradeCommandReviewCommentHandler(Services.IGitHubClientFactory gitHubClientFactory, Services.IGradeStore gradeStore, IMemoryCache cache, Microsoft.Extensions.Logging.ILogger logger)
            : base(gitHubClientFactory, gradeStore, cache, logger)
        {
        }

        protected override async Task<EventHandlerResult> executeCore(PullRequestReviewEventPayload webhookPayload)
        {
            if (webhookPayload.Review == null)
                return EventHandlerResult.PayloadError("no review information in webhook payload");

            if (webhookPayload.Action.Equals("submitted", StringComparison.OrdinalIgnoreCase))
                return await processComment(new ReviewCommentPayloadFacade(webhookPayload));

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }

        protected override Task handleReaction(ICommentPayload<PullRequestReviewEventPayload> webhookPayload, ReactionType reactionType)
        {
            // The implementation below does not work, GitHub API returns an error indicating that the resource is not available for the GitHub App
            // According to documentation "pull request" read/write permission should be enough to add the reaction, but it seems insufficient.
            return Task.CompletedTask;

            /*
            // discrepancy in the Octokit library regarding long-int ids
            if (webhookPayload.Payload.Review.Id > int.MaxValue)
                return Task.CompletedTask;

            return GitHubClient.Reaction.PullRequestReviewComment.Create(webhookPayload.Repository.Id, (int)webhookPayload.Payload.Review.Id, new NewReaction(reactionType));
            */
        }
    }
}
