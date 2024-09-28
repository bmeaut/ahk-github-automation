using System;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class PullRequestStatusTrackingHandler(
        IGitHubClientFactory gitHubClientFactory,
        IStatusTrackingStore statusTrackingStore,
        IMemoryCache cache,
        ILogger logger) : RepositoryEventBase<PullRequestEventPayload>(gitHubClientFactory, cache, logger)
    {
        public const string GitHubWebhookEventName = "pull_request";

        protected override async Task<EventHandlerResult> executeCore(PullRequestEventPayload webhookPayload)
        {
            if (webhookPayload.PullRequest == null)
                return EventHandlerResult.PayloadError("no pull request information in webhook payload");

            if (webhookPayload.Action.Equals("opened", StringComparison.OrdinalIgnoreCase) ||
                webhookPayload.Action.Equals("assigned", StringComparison.OrdinalIgnoreCase) ||
                webhookPayload.Action.Equals("review_requested", StringComparison.OrdinalIgnoreCase) ||
                webhookPayload.Action.Equals("closed", StringComparison.OrdinalIgnoreCase))
                return await processPullRequestEvent(webhookPayload);

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }

        private async Task<EventHandlerResult> processPullRequestEvent(PullRequestEventPayload webhookPayload)
        {
            var repository = webhookPayload.Repository.FullName;
            var action = webhookPayload.Action;
            var assignees = webhookPayload.PullRequest.Assignees?.Select(u => u.Login)?.ToArray();
            var neptun = await getNeptun(webhookPayload.Repository.Id, webhookPayload.PullRequest.Head.Ref);

            await statusTrackingStore.StoreEvent(new PullRequestEvent(
                repository: repository,
                timestamp: DateTime.UtcNow,
                action: action,
                assignees: assignees,
                neptun: neptun,
                htmlUrl: webhookPayload.PullRequest.HtmlUrl,
                number: webhookPayload.PullRequest.Number));

            return EventHandlerResult.ActionPerformed("pull request lifecycle handled");
        }
    }
}
