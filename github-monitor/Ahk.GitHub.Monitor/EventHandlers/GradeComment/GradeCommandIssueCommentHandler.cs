using Ahk.GitHub.Monitor.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Octokit;
using System;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class GradeCommandIssueCommentHandler : GradeCommandHandlerBase<IssueCommentPayload>
    {
        public const string GitHubWebhookEventName = "issue_comment";

        public GradeCommandIssueCommentHandler(Services.IGitHubClientFactory gitHubClientFactory, Services.IGradeStore gradeStore, IMemoryCache cache)
            : base(gitHubClientFactory, gradeStore, cache)
        {
        }

        protected override async Task<EventHandlerResult> executeCore(IssueCommentPayload webhookPayload)
        {
            if (webhookPayload.Issue == null)
                return EventHandlerResult.PayloadError("no issue information in webhook payload");

            if (webhookPayload.Action.Equals("created", StringComparison.OrdinalIgnoreCase))
                return await processComment(new IssueCommentPayloadFacade(webhookPayload));

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }

        protected override Task handleReaction(ICommentPayload<IssueCommentPayload> webhookPayload, ReactionType reactionType)
            => GitHubClient.Reaction.IssueComment.Create(webhookPayload.Repository.Id, webhookPayload.Payload.Comment.Id, new NewReaction(reactionType));
    }
}
