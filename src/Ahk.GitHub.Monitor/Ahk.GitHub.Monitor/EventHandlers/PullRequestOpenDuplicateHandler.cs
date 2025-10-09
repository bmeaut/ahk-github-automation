using Ahk.GitHub.Monitor.EventHandlers.Abstractions;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers;

public class PullRequestOpenDuplicateHandler(
    IGitHubClientFactory gitHubClientFactory,
    IMemoryCache cache,
    ILogger<PullRequestOpenDuplicateHandler> logger)
    : RepositoryEventHandlerBase<PullRequestEventPayload>(gitHubClientFactory, cache, logger), IGitHubEventHandler
{
    public static string GitHubWebhookEventName => "pull_request";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(PullRequestEventPayload webhookPayload)
    {
        if (webhookPayload.PullRequest == null)
        {
            return EventHandlerResult.PayloadError("no pull request information in webhook payload");
        }

        if (webhookPayload.Action.Equals("opened", StringComparison.OrdinalIgnoreCase))
        {
            var repositoryPrs = await GitHubClient.PullRequest.GetAllForRepository(webhookPayload.Repository.Id, new PullRequestRequest() { State = ItemStateFilter.All });
            if (repositoryPrs.Count <= 1)
            {
                return EventHandlerResult.NoActionNeeded("pull request open is ok, there are no other PRs");
            }

            var (handledOpen, resultOpen) = await HandleAnyOpenPrsAsync(webhookPayload, repositoryPrs);
            var (handledClosed, resultClosed) = await HandleAnyClosedPrsAsync(webhookPayload, repositoryPrs);

            if (!handledOpen && !handledClosed)
            {
                return EventHandlerResult.NoActionNeeded($"{resultOpen}; {resultClosed}");
            }

            return EventHandlerResult.ActionPerformed($"{resultOpen}; {resultClosed}");
        }

        return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
    }

    private static string GetWarningText(int currentPrNumber, IEnumerable<int> foundPrNumbers)
    {
        var prReferencesText = string.Join(" ",
            foundPrNumbers
                .Union([currentPrNumber])
                .Distinct()
                .OrderBy(num => num)
                .Select(n => $"#{n}"));

        return $":exclamation: **You have multiple pull requests. Tobb pull request-et nyitottal.** {prReferencesText} \n\n";
    }

    private async Task<(bool HasProblem, string ResultText)> HandleAnyOpenPrsAsync(PullRequestEventPayload webhookPayload, IReadOnlyCollection<PullRequest> repositoryPrs)
    {
        var openPrs = repositoryPrs.Where(otherPr => otherPr.State == ItemState.Open).ToList();
        if (openPrs.Count > 1)
        {
            var warningText = GetWarningText(webhookPayload.PullRequest.Number, openPrs.Select(pr => pr.Number));
            foreach (var openPullRequest in openPrs)
            {
                await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, openPullRequest.Number, warningText);
            }

            return (true, "pull request open handled with multiple open PRs");
        }

        return (false, "pull request open is ok, there are no other open PRs");
    }

    private async Task<(bool HasProblem, string ResultText)> HandleAnyClosedPrsAsync(PullRequestEventPayload webhookPayload, IReadOnlyCollection<PullRequest> repositoryPrs)
    {
        var closedPrs = repositoryPrs.Where(otherPr => otherPr.State == ItemState.Closed).ToList();
        if (closedPrs.Count != 0)
        {
            var prsClosedByNotStudent = new List<int>();
            foreach (var otherClosedPr in closedPrs)
            {
                if (await IsPrClosedByNotStudent(webhookPayload, otherClosedPr))
                {
                    prsClosedByNotStudent.Add(otherClosedPr.Number);
                }
            }

            if (prsClosedByNotStudent.Count != 0)
            {
                var warningText = GetWarningText(webhookPayload.PullRequest.Number, prsClosedByNotStudent);
                await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Number, warningText);

                return (true, "pull request open handled with already closed PRs");
            }

            return (false, "pull request open is ok, there are no other evaluated PRs");
        }

        return (false, "pull request open is ok, there are no other closed PRs");
    }

    private async Task<bool> IsPrClosedByNotStudent(PullRequestEventPayload webhookPayload, PullRequest pr)
    {
        var issueEvents = await GitHubClient.Issue.Events.GetAllForIssue(webhookPayload.Repository.Id, pr.Number);

        // PR closed by someone other than the person who opened it -> student opened and teached closed PR
        return issueEvents.Any(e => e.Event.Value == EventInfoState.Closed && e.Actor?.Id != pr.User.Id);
    }
}
