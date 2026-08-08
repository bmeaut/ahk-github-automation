using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers;

/// <summary>
/// Warns when a student opens more than one pull request for the same work — several open at once, or a new
/// one after a teacher already closed an earlier one. Ported from
/// <c>github-monitor/.../EventHandlers/PullRequestOpenDuplicateHandler.cs</c>.
///
/// ⚠️ The slowest handler by some way: it lists every pull request in the repository and, for each closed one,
/// its issue events. On a repository with a long history this alone can approach GitHub's delivery timeout.
/// </summary>
public sealed class PullRequestOpenDuplicateHandler : RepositoryEventHandlerBase<PullRequestEventPayload>
{
    private const string WarningText = ":exclamation: **You have multiple pull requests. Tobb pull request-et nyitottal.** {} \n\n";

    public PullRequestOpenDuplicateHandler(IMemoryCache cache, ILogger<PullRequestOpenDuplicateHandler> logger)
        : base(cache, logger)
    {
    }

    public override string GitHubEventName => "pull_request";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(GitHubWebhookContext context, PullRequestEventPayload payload, CancellationToken cancellationToken)
    {
        if (payload.PullRequest is null)
            return EventHandlerResult.PayloadError("no pull request information in webhook payload");

        if (!payload.Action.Equals("opened", StringComparison.OrdinalIgnoreCase))
            return EventHandlerResult.EventNotOfInterest(payload.Action);

        var repositoryPrs = await context.GitHubClient.PullRequest.GetAllForRepository(
            payload.Repository.Id, new PullRequestRequest { State = ItemStateFilter.All });

        if (repositoryPrs.Count <= 1)
            return EventHandlerResult.NoActionNeeded("pull request open is ok, there are no other PRs");

        var (handledOpen, resultOpen) = await HandleAnyOpenPrsAsync(context, payload, repositoryPrs);
        var (handledClosed, resultClosed) = await HandleAnyClosedPrsAsync(context, payload, repositoryPrs);

        return !handledOpen && !handledClosed
            ? EventHandlerResult.NoActionNeeded($"{resultOpen}; {resultClosed}")
            : EventHandlerResult.ActionPerformed($"{resultOpen}; {resultClosed}");
    }

    private static string GetWarningText(int currentPrNumber, IEnumerable<int> foundPrNumbers)
    {
        var prReferencesText = string.Join(
            " ",
            foundPrNumbers.Union(new[] { currentPrNumber }).Distinct().OrderBy(num => num).Select(n => $"#{n}").ToArray());

        return WarningText.Replace("{}", prReferencesText, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(bool HasProblem, string ResultText)> HandleAnyOpenPrsAsync(
        GitHubWebhookContext context, PullRequestEventPayload payload, IReadOnlyCollection<PullRequest> repositoryPrs)
    {
        var openPrs = repositoryPrs.Where(otherPr => otherPr.State == ItemState.Open).ToList();
        if (openPrs.Count <= 1)
            return (false, "pull request open is ok, there are no other open PRs");

        var warningText = GetWarningText(payload.PullRequest.Number, openPrs.Select(pr => pr.Number));
        foreach (var openPullRequest in openPrs)
            await context.GitHubClient.Issue.Comment.Create(payload.Repository.Id, openPullRequest.Number, warningText);

        return (true, "pull request open handled with multiple open PRs");
    }

    private static async Task<(bool HasProblem, string ResultText)> HandleAnyClosedPrsAsync(
        GitHubWebhookContext context, PullRequestEventPayload payload, IReadOnlyCollection<PullRequest> repositoryPrs)
    {
        var closedPrs = repositoryPrs.Where(otherPr => otherPr.State == ItemState.Closed).ToList();
        if (closedPrs.Count == 0)
            return (false, "pull request open is ok, there are no other closed PRs");

        var prsClosedByNotStudent = new List<int>();
        foreach (var otherClosedPr in closedPrs)
        {
            if (await IsPrClosedByNotStudentAsync(context, payload, otherClosedPr))
                prsClosedByNotStudent.Add(otherClosedPr.Number);
        }

        if (prsClosedByNotStudent.Count == 0)
            return (false, "pull request open is ok, there are no other evaluated PRs");

        var warningText = GetWarningText(payload.PullRequest.Number, prsClosedByNotStudent);
        await context.GitHubClient.Issue.Comment.Create(payload.Repository.Id, payload.Number, warningText);

        return (true, "pull request open handled with already closed PRs");
    }

    private static async Task<bool> IsPrClosedByNotStudentAsync(GitHubWebhookContext context, PullRequestEventPayload payload, PullRequest pr)
    {
        var issueEvents = await context.GitHubClient.Issue.Events.GetAllForIssue(payload.Repository.Id, pr.Number);

        // A PR the student opened and somebody else closed is one a teacher already evaluated.
        return issueEvents.Any(e => e.Event.Value == EventInfoState.Closed && e.Actor?.Id != pr.User.Id);
    }
}
