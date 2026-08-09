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
        if (!await IsAllowedAsync(context, payload))
            return await HandleUserNotAllowedAsync(context, payload);

        var pr = await GetPullRequestAsync(context, payload);
        if (pr is null)
            return await HandleNotPrAsync(context, payload);

        await HandleApproveAsync(context, payload, pr);
        await HandleStoreGradeAsync(context, payload, gradeCommand, pr, cancellationToken);

        await HandleReactionAsync(context, payload, ReactionType.Plus1);
        return EventHandlerResult.ActionPerformed($"comment operation to grade done; grades: {string.Join(" ", gradeCommand.Grades)}");
    }

    private static async Task<PullRequest?> GetPullRequestAsync(GitHubWebhookContext context, ICommentPayload<T> payload)
    {
        try
        {
            return await context.GitHubClient.PullRequest.Get(payload.Repository.Id, payload.PullRequestNumber);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private async Task HandleStoreGradeAsync(GitHubWebhookContext context, ICommentPayload<T> payload, GradeCommentParser gradeCommand, PullRequest pr, CancellationToken cancellationToken)
    {
        var neptun = await GetNeptunAsync(context, payload.Repository.Id, pr.Head.Ref);
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

    private async Task HandleApproveAsync(GitHubWebhookContext context, ICommentPayload<T> payload, PullRequest pr)
    {
        if (pr.State.Value == ItemState.Open && pr.Mergeable == true)
        {
            Logger.LogInformation("PR is being merged");
            await context.GitHubClient.PullRequest.Review.Create(
                payload.Repository.Id, payload.PullRequestNumber, new PullRequestReviewCreate { Event = PullRequestReviewEvent.Approve });
            await context.GitHubClient.PullRequest.Merge(payload.Repository.Id, payload.PullRequestNumber, new MergePullRequest());
        }
        else
        {
            Logger.LogInformation("PR is not mergable");
        }
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

    private Task<bool> IsAllowedAsync(GitHubWebhookContext context, ICommentPayload<T> payload)
        => payload.Repository.Owner.Type != AccountType.Organization
            ? Task.FromResult(false)
            : IsOrganizationMemberAsync(context, payload.Repository.Owner.Login, payload.CommentingUser);
}
