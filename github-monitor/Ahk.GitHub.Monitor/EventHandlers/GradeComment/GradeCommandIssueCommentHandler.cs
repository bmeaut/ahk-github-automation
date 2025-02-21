using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers.BaseAndUtils;
using Ahk.GitHub.Monitor.EventHandlers.GradeComment.Payload;
using Ahk.GitHub.Monitor.Services;
using Ahk.GitHub.Monitor.Services.GradeStore;
using Microsoft.Extensions.Caching.Memory;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers.GradeComment;

public class GradeCommandIssueCommentHandler(
    IGitHubClientFactory gitHubClientFactory,
    IGradeStore gradeStore,
    IMemoryCache cache,
    IServiceProvider serviceProvider,
    PullRequestStatusTrackingHandler pullRequestStatusTrackingHandler)
    : GradeCommandHandlerBase<IssueCommentPayload>(gitHubClientFactory, gradeStore, cache, serviceProvider,
        pullRequestStatusTrackingHandler)
{
    public const string GitHubWebhookEventName = "issue_comment";

    protected override async Task<EventHandlerResult> executeCore(IssueCommentPayload webhookPayload)
    {
        if (webhookPayload.Issue == null)
        {
            return EventHandlerResult.PayloadError("no issue information in webhook payload");
        }

        if (webhookPayload.Action.Equals("created", StringComparison.OrdinalIgnoreCase))
        {
            return await this.processComment(new IssueCommentPayloadFacade(webhookPayload));
        }

        return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
    }

    protected override Task handleReaction(
        ICommentPayload<IssueCommentPayload> webhookPayload, ReactionType reactionType)
        => this.GitHubClient.Reaction.IssueComment.Create(
            webhookPayload.Repository.Id,
            webhookPayload.Payload.Comment.Id, new NewReaction(reactionType));
}
