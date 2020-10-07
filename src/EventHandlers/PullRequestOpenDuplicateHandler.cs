using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class PullRequestOpenDuplicateHandler : RepositoryEventBase<PullRequestEventPayload>
    {
        public const string GitHubWebhookEventName = "pull_request";
        private const string DefaultWarningText = ":exclamation: **You have multiple pull requests. Tobb pull request-et nyitottal.** {} \n\n _This is an automated message. Ez egy automata uzenet._";

        public PullRequestOpenDuplicateHandler(Services.IGitHubClientFactory gitHubClientFactory)
            : base(gitHubClientFactory)
        {
        }

        protected override async Task execute(PullRequestEventPayload webhookPayload, RepositorySettings repoSettings, WebhookResult webhookResult)
        {
            if (webhookPayload.PullRequest == null)
            {
                webhookResult.LogError("no pull request information in webhook payload");
            }
            else if (repoSettings.MultiplePRProtection == null || !repoSettings.MultiplePRProtection.Enabled)
            {
                webhookResult.LogError("multiple PR protection not enabled for repository");
            }
            else if (webhookPayload.Action.Equals("opened", StringComparison.OrdinalIgnoreCase))
            {
                var repositoryPrs = await getPullRequestsExceptCurrent(webhookPayload);
                if (repositoryPrs.Count == 0)
                {
                    webhookResult.LogInfo("pull request open is ok, there are no other PRs");
                }
                else
                {
                    await handleAnyOpenPrs(webhookPayload, repoSettings, webhookResult, repositoryPrs);
                    await handleAnyClosedPrs(webhookPayload, repoSettings, webhookResult, repositoryPrs);
                }
            }
            else
            {
                webhookResult.LogInfo($"pull request action {webhookPayload.Action} is not of interrest");
            }
        }

        private async Task<IReadOnlyCollection<PullRequest>> getPullRequestsExceptCurrent(PullRequestEventPayload webhookPayload)
        {
            var allPullRequests = await GitHubClient.PullRequest.GetAllForRepository(webhookPayload.Repository.Id, new PullRequestRequest() { State = ItemStateFilter.All });
            return allPullRequests.Where(otherPr => otherPr.Number != webhookPayload.PullRequest.Number).ToList();
        }

        private async Task handleAnyOpenPrs(PullRequestEventPayload webhookPayload, RepositorySettings repoSettings, WebhookResult webhookResult, IReadOnlyCollection<PullRequest> repositoryPrs)
        {
            var openPrs = repositoryPrs.Where(otherPr => otherPr.State == ItemState.Open).ToList();
            if (openPrs.Any())
            {
                var warningText = getWarningText(repoSettings.MultiplePRProtection, webhookPayload.PullRequest.Number, openPrs.Select(pr => pr.Number));
                foreach (var openPullRequest in openPrs)
                    await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, openPullRequest.Number, warningText);

                webhookResult.LogInfo("pull request open handled with multiple open PRs");
            }
            else
            {
                webhookResult.LogInfo("pull request open is ok, there are no other open PRs");
            }
        }

        private async Task handleAnyClosedPrs(PullRequestEventPayload webhookPayload, RepositorySettings repoSettings, WebhookResult webhookResult, IReadOnlyCollection<PullRequest> repositoryPrs)
        {
            var closedPrs = repositoryPrs.Where(otherPr => otherPr.State == ItemState.Closed).ToList();
            if (closedPrs.Any())
            {
                var prsClosedByNotStudent = new List<int>();
                foreach (var otherClosedPr in closedPrs)
                {
                    if (await isPrClosedByNotStudent(webhookPayload, otherClosedPr))
                        prsClosedByNotStudent.Add(otherClosedPr.Number);
                }

                if (prsClosedByNotStudent.Any())
                {
                    var warningText = getWarningText(repoSettings.MultiplePRProtection, webhookPayload.PullRequest.Number, prsClosedByNotStudent);
                    await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Number, warningText);

                    webhookResult.LogInfo("pull request open handled with already closed PRs");
                }
                else
                {
                    webhookResult.LogInfo("pull request open is ok, there are no other evaluated PRs");
                }
            }
            else
            {
                webhookResult.LogInfo("pull request open is ok, there are no other closed PRs");
            }
        }

        private async Task<bool> isPrClosedByNotStudent(PullRequestEventPayload webhookPayload, PullRequest pr)
        {
            var issueEvents = await GitHubClient.Issue.Events.GetAllForIssue(webhookPayload.Repository.Id, pr.Number);
            return issueEvents.Any(
                e => e.Event.Value == EventInfoState.Closed // closed event
                     && e.Actor?.Id != pr.User.Id); // PR closed by someone other than the person who opened it -> student opened and teached closed PR
        }

        private static string getWarningText(MultiplePRProtectionSettings multiplePRProtection, int currentPrNumber, IEnumerable<int> foundPrNumbers)
        {
            var prReferencesText = string.Join(" ", foundPrNumbers.Union(new[] { currentPrNumber }).Distinct().OrderBy(num => num).Select(n => $"#{n}").ToArray());
            var effectiveWarningText = string.IsNullOrEmpty(multiplePRProtection.WarningText) ? DefaultWarningText : multiplePRProtection.WarningText;
            return effectiveWarningText.Replace("{}", prReferencesText);
        }
    }
}
