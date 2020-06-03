using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class PullRequestEventHandler : RepositoryEventBase<PullRequestEventPayload>
    {
        public const string GitHubWebhookEventName = "pull_request";
        public const string FeatureFlagName = "AHK_ONEPULLREQUEST_ENABLED";

        public PullRequestEventHandler()
            : base(FeatureFlagName)
        {
        }

        protected override async Task execute(GitHubClient gitHubClient, PullRequestEventPayload webhookPayload, WebhookResult webhookResult)
        {
            if (webhookPayload.PullRequest == null)
            {
                webhookResult.LogError("no pull request information in webhook payload");
            }
            else if (webhookPayload.Action.Equals("opened", StringComparison.OrdinalIgnoreCase))
            {
                var openPullRequests = await gitHubClient.PullRequest.GetAllForRepository(webhookPayload.Repository.Id, new PullRequestRequest() { State = ItemStateFilter.Open });
                if (openPullRequests.Count > 1)
                {
                    var warningText = getWarningText(openPullRequests.Select(pr => pr.Number));
                    foreach (var openPullRequest in openPullRequests)
                        await gitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, openPullRequest.Number, warningText);

                    webhookResult.LogInfo("pull request open handled with multiple open PRs");
                }
                else
                {
                    webhookResult.LogInfo($"pull request open is ok, there are no other open PRs");
                }

                var allRepoEvents = await gitHubClient.Issue.Events.GetAllForRepository(webhookPayload.Repository.Id);
                var prsClosedByNotStudent = allRepoEvents.Where(e =>
                        e.Event.Value == EventInfoState.Closed // closed event
                        && e.Issue != null && e.Issue.PullRequest != null // related to a PR
                        && e.Actor.Id != e.Issue.User.Id) // PR closed by someone other than the person who opened it -> student opened and teached closed PR
                    .ToList();
                if (prsClosedByNotStudent.Count > 0)
                {
                    var warningText = getWarningText(prsClosedByNotStudent.Select(e => e.Issue.Number));
                    await gitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Number, warningText);

                    webhookResult.LogInfo("pull request open handled with alrady closed PRs");
                }
                else
                {
                    webhookResult.LogInfo($"pull request open is ok, there are no other evaluated PRs");
                }
            }
            else
            {
                webhookResult.LogInfo($"pull request action {webhookPayload.Action} is not of interrest");
            }
        }

        private static string getWarningText(IEnumerable<int> referencedPullRequestNumbers)
        {
            string prReferencesText = getPRReferencesTextForComment(referencedPullRequestNumbers);
            var commentMsg = Environment.GetEnvironmentVariable("AHK_ONEPULLREQUEST_MESSAGE", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(commentMsg) || string.IsNullOrWhiteSpace(commentMsg))
                return prReferencesText + @" :exclamation: **You have multiple pull requests. Tobb pull request-et nyitottal.** \n\n _This is an automated message. Ez egy automata uzenet._";
            else
                return prReferencesText + " " + commentMsg;
        }

        private static string getPRReferencesTextForComment(IEnumerable<int> referencedPullRequestNumbers)
            => string.Join(" ", referencedPullRequestNumbers.Select(n => $"#{n}").ToArray());
    }
}
