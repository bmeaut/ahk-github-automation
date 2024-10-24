using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class PullRequestOpenDuplicateHandler(
        IGitHubClientFactory gitHubClientFactory,
        IMemoryCache cache,
        IServiceProvider serviceProvider)
        : RepositoryEventBase<PullRequestEventPayload>(gitHubClientFactory, cache, serviceProvider)
    {
        public const string GitHubWebhookEventName = "pull_request";

        private const string WarningText =
            ":exclamation: **You have multiple pull requests. Tobb pull request-et nyitottal.** {} \n\n";

        protected override async Task<EventHandlerResult> executeCore(PullRequestEventPayload webhookPayload)
        {
            if (webhookPayload.PullRequest == null)
                return EventHandlerResult.PayloadError("no pull request information in webhook payload");

            if (webhookPayload.Action.Equals("opened", StringComparison.OrdinalIgnoreCase))
            {
                var repositoryPrs = await getAllRepoPullRequests(webhookPayload);
                if (repositoryPrs.Count <= 1)
                {
                    return EventHandlerResult.NoActionNeeded("pull request open is ok, there are no other PRs");
                }

                var (handledOpen, resultOpen) = await this.handleAnyOpenPrs(webhookPayload, repositoryPrs);
                var (handledClosed, resultClosed) = await this.handleAnyClosedPrs(webhookPayload, repositoryPrs);

                if (!handledOpen && !handledClosed)
                {
                    return EventHandlerResult.NoActionNeeded($"{resultOpen}; {resultClosed}");
                }

                return EventHandlerResult.ActionPerformed($"{resultOpen}; {resultClosed}");
            }

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }

        private static string getWarningText(int currentPrNumber, IEnumerable<int> foundPrNumbers)
        {
            var prReferencesText = string.Join(" ",
                foundPrNumbers.Union(new[] { currentPrNumber }).Distinct().OrderBy(num => num).Select(n => $"#{n}")
                    .ToArray());
            return WarningText.Replace("{}", prReferencesText, StringComparison.OrdinalIgnoreCase);
        }

        private Task<IReadOnlyList<PullRequest>> getAllRepoPullRequests(PullRequestEventPayload webhookPayload)
            => GitHubClient.PullRequest.GetAllForRepository(webhookPayload.Repository.Id,
                new PullRequestRequest() { State = ItemStateFilter.All });

        private async Task<(bool HasProblem, string ResultText)> handleAnyOpenPrs(
            PullRequestEventPayload webhookPayload, IReadOnlyCollection<PullRequest> repositoryPrs)
        {
            var openPrs = repositoryPrs.Where(otherPr => otherPr.State == ItemState.Open).ToList();
            if (openPrs.Count > 1)
            {
                var warningText = getWarningText(webhookPayload.PullRequest.Number, openPrs.Select(pr => pr.Number));
                foreach (var openPullRequest in openPrs)
                    await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, openPullRequest.Number,
                        warningText);

                return (true, "pull request open handled with multiple open PRs");
            }

            return (false, "pull request open is ok, there are no other open PRs");
        }

        private async Task<(bool HasProblem, string ResultText)> handleAnyClosedPrs(
            PullRequestEventPayload webhookPayload, IReadOnlyCollection<PullRequest> repositoryPrs)
        {
            var closedPrs = repositoryPrs.Where(otherPr => otherPr.State == ItemState.Closed).ToList();
            if (closedPrs.Count != 0)
            {
                var prsClosedByNotStudent = new List<int>();
                foreach (var otherClosedPr in closedPrs)
                {
                    if (await isPrClosedByNotStudent(webhookPayload, otherClosedPr))
                        prsClosedByNotStudent.Add(otherClosedPr.Number);
                }

                if (prsClosedByNotStudent.Count != 0)
                {
                    var warningText = getWarningText(webhookPayload.PullRequest.Number, prsClosedByNotStudent);
                    await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Number,
                        warningText);

                    return (true, "pull request open handled with already closed PRs");
                }

                return (false, "pull request open is ok, there are no other evaluated PRs");
            }

            return (false, "pull request open is ok, there are no other closed PRs");
        }

        private async Task<bool> isPrClosedByNotStudent(PullRequestEventPayload webhookPayload, PullRequest pr)
        {
            var issueEvents = await GitHubClient.Issue.Events.GetAllForIssue(webhookPayload.Repository.Id, pr.Number);
            return issueEvents.Any(
                e => e.Event.Value == EventInfoState.Closed // closed event
                     && e.Actor?.Id !=
                     pr.User.Id); // PR closed by someone other than the person who opened it -> student opened and teached closed PR
        }
    }
}
