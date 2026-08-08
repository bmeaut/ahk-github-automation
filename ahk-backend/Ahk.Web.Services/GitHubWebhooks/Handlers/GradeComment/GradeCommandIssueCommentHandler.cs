using Ahk.Web.Services.Grading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers.GradeComment;

/// <summary><c>/ahk ok</c> posted as an ordinary pull request comment. The common case.</summary>
public sealed class GradeCommandIssueCommentHandler : GradeCommandHandlerBase<IssueCommentPayload>
{
    public GradeCommandIssueCommentHandler(IGradeService grades, IMemoryCache cache, ILogger<GradeCommandIssueCommentHandler> logger)
        : base(grades, cache, logger)
    {
    }

    public override string GitHubEventName => "issue_comment";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(GitHubWebhookContext context, IssueCommentPayload payload, CancellationToken cancellationToken)
    {
        if (payload.Issue is null)
            return EventHandlerResult.PayloadError("no issue information in webhook payload");

        if (payload.Action.Equals("created", StringComparison.OrdinalIgnoreCase))
            return await ProcessCommentAsync(context, new IssueCommentPayloadFacade(payload), cancellationToken);

        return EventHandlerResult.EventNotOfInterest(payload.Action);
    }

    protected override Task HandleReactionAsync(GitHubWebhookContext context, ICommentPayload<IssueCommentPayload> payload, ReactionType reactionType)
        => context.GitHubClient.Reaction.IssueComment.Create(payload.Repository.Id, payload.Payload.Comment.Id, new NewReaction(reactionType));
}
