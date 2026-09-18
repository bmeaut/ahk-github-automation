using Ahk.Web.Services.GitHub;
using Ahk.Web.Services.Grading;
using Ahk.Web.Services.Grading.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers.GradeComment;

/// <summary>
/// The <c>/ahk ok</c> chatops command: a teacher comment that approves a student's pull request, merges it, and
/// records the grade. Ported from <c>github-monitor/.../EventHandlers/GradeComment/GradeCommandHandlerBase.cs</c>.
///
/// <para>The one structural change: where the original enqueued a message onto Azure Queue Storage for
/// grade-management to pick up, this calls <see cref="IGradeService"/> directly. A failure is therefore visible
/// in the delivery log instead of disappearing into a queue — and because the 👍 reaction is added only after
/// the grade write, its presence remains an honest signal that the whole command succeeded.</para>
/// </summary>
public abstract class GradeCommandHandlerBase<T> : RepositoryEventHandlerBase<T>
    where T : ActivityPayload
{
    private const string WarningText = ":exclamation: **@{} is not allowed to do that. @{} Ez nem engedelyezett szamodra.**";

    /// <summary>
    /// Addressed to both parties at once, because the fix needs both: the student marks the pull request ready
    /// for review, and only then can the teacher issue the command again.
    /// </summary>
    private const string DraftWarningText = ":exclamation: **This pull request is still a draft, so it was not graded. Mark it ready for review, then ask for the grading again. Ez a pull request még piszkozat, ezért nem lett értékelve. Jelöld késznek (Ready for review), majd kérd újra az értékelést.**";

    private readonly IGradeService grades;

    protected GradeCommandHandlerBase(IGradeService grades, IMemoryCache cache, ILogger logger)
        : base(cache, logger)
    {
        this.grades = grades;
    }

    protected abstract Task HandleReactionAsync(GitHubWebhookContext context, ICommentPayload<T> payload, ReactionType reactionType);

    protected async Task<EventHandlerResult> ProcessCommentAsync(GitHubWebhookContext context, ICommentPayload<T> payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var gradeCommand = new GradeCommentParser(payload.CommentBody);
        if (!gradeCommand.IsMatch)
            return EventHandlerResult.NoActionNeeded("not recognized as command");

        // Only organization members may grade. A student posting "/ahk ok" in their own repository gets told so.
        if (!await IsAllowedAsync(context, payload, cancellationToken))
            return await HandleUserNotAllowedAsync(context, payload);

        var pr = await GetPullRequestAsync(context, payload, cancellationToken);
        if (pr is null)
            return await HandleNotPrAsync(context, payload);

        // ⚠️ Checked before anything at all is written. A draft is the student declaring the work unfinished, so
        // the whole command is refused rather than half-applied: no approving review, no merge, no grade. The
        // other unmergeable states are mechanical — conflicts, or GitHub still computing — and do not refuse
        // the grade; see HandleApproveAsync.
        if (pr.Draft)
            return await HandleDraftAsync(context, payload);

        // ⚠️ The grade is written before the merge, and the order is load-bearing twice over. The grade is the
        // half that cannot be recovered — a merge a teacher can redo by hand, a silently dropped grade nobody
        // ever learns about — and a production 405 on the merge endpoint cost exactly one that way. It also
        // keeps the neptun.txt read on the head branch ahead of the merge that may delete that branch.
        await HandleStoreGradeAsync(context, payload, gradeCommand, pr, cancellationToken);
        var notMergedReason = await HandleApproveAsync(context, payload, pr);

        await HandleReactionAsync(context, payload, ReactionType.Plus1);

        var grades = $"grades: {string.Join(" ", gradeCommand.Grades)}";
        return EventHandlerResult.ActionPerformed(notMergedReason is null
            ? $"comment operation to grade done; {grades}"
            : $"comment operation to grade done, but the pull request was not merged: {notMergedReason}; {grades}");
    }

    private async Task<PullRequest?> GetPullRequestAsync(GitHubWebhookContext context, ICommentPayload<T> payload, CancellationToken cancellationToken)
    {
        try
        {
            return await GitHubCallRetry.IdempotentAsync(
                "read pull request",
                () => context.GitHubClient.PullRequest.Get(payload.Repository.Id, payload.PullRequestNumber),
                Logger,
                cancellationToken);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private async Task HandleStoreGradeAsync(GitHubWebhookContext context, ICommentPayload<T> payload, GradeCommentParser gradeCommand, PullRequest pr, CancellationToken cancellationToken)
    {
        var neptun = await GetNeptunAsync(context, payload.Repository.Id, pr.Head.Ref, cancellationToken);
        Logger.LogInformation("storing grades for {Neptun}", neptun);

        if (gradeCommand.HasGrades)
        {
            await grades.SetGradeAsync(
                context.CourseId,
                new SetGradeInput
                {
                    Neptun = neptun ?? string.Empty,
                    Repository = payload.Repository.FullName,
                    PrNumber = pr.Number,
                    PrUrl = pr.HtmlUrl,
                    Actor = payload.CommentingUser,
                    Origin = payload.CommentHtmlUrl,
                    Results = gradeCommand.Grades,
                },
                cancellationToken);
        }
        else
        {
            await grades.ConfirmAutoGradeAsync(
                context.CourseId,
                new ConfirmAutoGradeInput
                {
                    Neptun = neptun ?? string.Empty,
                    Repository = payload.Repository.FullName,
                    PrNumber = pr.Number,
                    PrUrl = pr.HtmlUrl,
                    Actor = payload.CommentingUser,
                    Origin = payload.CommentHtmlUrl,
                },
                cancellationToken);
        }
    }

    /// <summary>
    /// Approves and merges, returning why it did not when it did not — null meaning it merged. The reason
    /// reaches the handler result and so the delivery log: "not mergable" used to be an <c>ILogger</c> line and
    /// nothing else, which left a skipped merge indistinguishable from a successful one.
    ///
    /// <para>⚠️ Neither call here is retried, and neither may become so. A timeout is not proof that GitHub did
    /// nothing: a 504 on the merge endpoint commonly means the merge was slow, not that it was refused, so a
    /// second attempt can merge twice or leave a duplicate approving review. When this throws, the delivery is
    /// recorded as failed and an administrator decides — which is the whole reason handler failures are not
    /// retried automatically at the delivery level either. The grade is safely written by then.</para>
    /// </summary>
    private async Task<string?> HandleApproveAsync(GitHubWebhookContext context, ICommentPayload<T> payload, PullRequest pr)
    {
        if (pr.State.Value != ItemState.Open)
        {
            Logger.LogInformation("PR is not mergable");
            return "it is no longer open";
        }

        // ⚠️ Mergeable is GitHub's *conflict* check, not permission to merge, and it is computed
        // asynchronously — so null means "not worked out yet", a race rather than an answer. It is also true
        // for a draft pull request, which is why draft is caught by the caller and never reaches here.
        if (pr.Mergeable != true)
        {
            Logger.LogInformation("PR is not mergable");
            return pr.Mergeable is null
                ? "GitHub had not finished working out whether it can be merged"
                : "it has conflicts that have to be resolved first";
        }

        Logger.LogInformation("PR is being merged");
        await context.GitHubClient.PullRequest.Review.Create(
            payload.Repository.Id, payload.PullRequestNumber, new PullRequestReviewCreate { Event = PullRequestReviewEvent.Approve });
        await context.GitHubClient.PullRequest.Merge(payload.Repository.Id, payload.PullRequestNumber, new MergePullRequest());

        return null;
    }

    /// <summary>
    /// A draft pull request refuses the whole command. Shaped exactly like
    /// <see cref="HandleUserNotAllowedAsync"/>: the confused reaction is this codebase's idiom for "your
    /// command was not acted on", and the comment is the only signal on the review path, where reacting is a
    /// deliberate no-op.
    ///
    /// <para>Reported as <c>ActionPerformed</c> rather than a failure: the delivery did exactly what it should
    /// have, and red in the admin console has to keep meaning that something went wrong.</para>
    /// </summary>
    private async Task<EventHandlerResult> HandleDraftAsync(GitHubWebhookContext context, ICommentPayload<T> payload)
    {
        Logger.LogInformation("PR is a draft, grading refused");

        await HandleReactionAsync(context, payload, ReactionType.Confused);
        await context.GitHubClient.Issue.Comment.Create(payload.Repository.Id, payload.PullRequestNumber, DraftWarningText);

        return EventHandlerResult.ActionPerformed("not graded: the pull request is still a draft");
    }

    private async Task<EventHandlerResult> HandleNotPrAsync(GitHubWebhookContext context, ICommentPayload<T> payload)
    {
        await HandleReactionAsync(context, payload, ReactionType.Confused);
        return EventHandlerResult.ActionPerformed("comment operation to grade not called for PR");
    }

    private async Task<EventHandlerResult> HandleUserNotAllowedAsync(GitHubWebhookContext context, ICommentPayload<T> payload)
    {
        await HandleReactionAsync(context, payload, ReactionType.Confused);

        var comment = WarningText.Replace("{}", payload.CommentingUser, StringComparison.OrdinalIgnoreCase);
        await context.GitHubClient.Issue.Comment.Create(payload.Repository.Id, payload.PullRequestNumber, comment);

        return EventHandlerResult.ActionPerformed("comment operation to grade not allowed for user");
    }

    private Task<bool> IsAllowedAsync(GitHubWebhookContext context, ICommentPayload<T> payload, CancellationToken cancellationToken)
        => payload.Repository.Owner.Type != AccountType.Organization
            ? Task.FromResult(false)
            : IsOrganizationMemberAsync(context, payload.Repository.Owner.Login, payload.CommentingUser, cancellationToken);
}
