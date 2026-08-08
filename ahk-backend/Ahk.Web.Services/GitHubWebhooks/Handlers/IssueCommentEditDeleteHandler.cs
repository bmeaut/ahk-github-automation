using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers;

/// <summary>
/// Leaves a visible trace when somebody edits or deletes another person's comment — the audit trail of a
/// graded pull request is part of the evaluation. Editing your own comment is fine. Ported from
/// <c>github-monitor/.../EventHandlers/IssueCommentEditDeleteHandler.cs</c>.
/// </summary>
public sealed class IssueCommentEditDeleteHandler : RepositoryEventHandlerBase<IssueCommentPayload>
{
    private const string WarningText = ":exclamation: **An issue comment was deleted / edited. Egy megjegyzes torolve vagy modositva lett.**";

    public IssueCommentEditDeleteHandler(IMemoryCache cache, ILogger<IssueCommentEditDeleteHandler> logger)
        : base(cache, logger)
    {
    }

    public override string GitHubEventName => "issue_comment";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(GitHubWebhookContext context, IssueCommentPayload payload, CancellationToken cancellationToken)
    {
        if (payload.Issue is null)
            return EventHandlerResult.PayloadError("no issue information in webhook payload");

        if (payload.Action.Equals("edited", StringComparison.OrdinalIgnoreCase) || payload.Action.Equals("deleted", StringComparison.OrdinalIgnoreCase))
        {
            if (payload.Sender is not null && payload.Comment?.User is not null && payload.Sender.Login == payload.Comment.User.Login)
                return EventHandlerResult.NoActionNeeded($"comment action {payload.Action} by {payload.Sender.Login} allowed, referencing own comment");

            await context.GitHubClient.Issue.Comment.Create(payload.Repository.Id, payload.Issue.Number, WarningText);
            return EventHandlerResult.ActionPerformed("comment action resulting in warning");
        }

        return EventHandlerResult.EventNotOfInterest(payload.Action);
    }
}
