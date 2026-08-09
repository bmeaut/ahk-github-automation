using Ahk.Web.Services.Grading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers.GradeComment;

/// <summary><c>/ahk ok</c> written in the body of a submitted pull request review rather than as a comment.</summary>
public sealed class GradeCommandReviewCommentHandler : GradeCommandHandlerBase<PullRequestReviewEventPayload>
{
    public GradeCommandReviewCommentHandler(IGradeService grades, IMemoryCache cache, ILogger<GradeCommandReviewCommentHandler> logger)
        : base(grades, cache, logger)
    {
    }

    public override string GitHubEventName => "pull_request_review";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(GitHubWebhookContext context, PullRequestReviewEventPayload payload, CancellationToken cancellationToken)
    {
        if (payload.Review is null)
            return EventHandlerResult.PayloadError("no review information in webhook payload");

        if (payload.Action.Equals("submitted", StringComparison.OrdinalIgnoreCase))
            return await ProcessCommentAsync(context, new ReviewCommentPayloadFacade(payload), cancellationToken);

        return EventHandlerResult.EventNotOfInterest(payload.Action);
    }

    /// <summary>
    /// Deliberately does nothing. Reacting to a *review* returns an error for a GitHub App: the documentation
    /// says pull-request read/write should be enough, and it is not. Carried over from
    /// <c>github-monitor</c>, where the same attempt was made and reverted — so a teacher grading through a
    /// review gets no 👍, only the grade.
    /// </summary>
    protected override Task HandleReactionAsync(GitHubWebhookContext context, ICommentPayload<PullRequestReviewEventPayload> payload, ReactionType reactionType)
        => Task.CompletedTask;
}
