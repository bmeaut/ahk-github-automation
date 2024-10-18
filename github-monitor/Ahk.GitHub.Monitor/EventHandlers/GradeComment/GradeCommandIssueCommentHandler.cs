using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers.StatusTracking;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class GradeCommandIssueCommentHandler(
        IGitHubClientFactory gitHubClientFactory,
        IGradeStore gradeStore,
        IMemoryCache cache,
        ILogger<GitHubMonitorFunction> logger,
        PullRequestStatusTrackingHandler pullRequestStatusTrackingHandler)
        : GradeCommandHandlerBase<IssueCommentPayload>(gitHubClientFactory, gradeStore, cache, logger, pullRequestStatusTrackingHandler)
    {
        public const string GitHubWebhookEventName = "issue_comment";

        protected override async Task<EventHandlerResult> executeCore(IssueCommentPayload webhookPayload)
        {
            if (webhookPayload.Issue == null)
                return EventHandlerResult.PayloadError("no issue information in webhook payload");

            if (webhookPayload.Action.Equals("created", StringComparison.OrdinalIgnoreCase))
                return await processComment(new IssueCommentPayloadFacade(webhookPayload));

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }

        protected override Task handleReaction(ICommentPayload<IssueCommentPayload> webhookPayload,
            ReactionType reactionType)
            => GitHubClient.Reaction.IssueComment.Create(webhookPayload.Repository.Id,
                webhookPayload.Payload.Comment.Id, new NewReaction(reactionType));
    }
}
