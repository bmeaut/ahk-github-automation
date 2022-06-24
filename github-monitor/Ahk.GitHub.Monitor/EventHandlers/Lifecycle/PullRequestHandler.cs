using System;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class PullRequestHandler : RepositoryEventBase<PullRequestEventPayload>
    {
        public const string GitHubWebhookEventName = "pull_request";
        private readonly ILifecycleStore lifecycleStore;

        public PullRequestHandler(IGitHubClientFactory gitHubClientFactory, ILifecycleStore lifecycleStore, IMemoryCache cache, ILogger logger)
            : base(gitHubClientFactory, cache, logger)
        {
            this.lifecycleStore = lifecycleStore;
        }

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
            var username = getGitHubUserNameFromRepositoryName(webhookPayload.Repository.FullName);
            var action = webhookPayload.Action;
            var assignees = webhookPayload.PullRequest.Assignees?.Select(u => u.Login)?.ToArray();
            var neptun = await getNeptun(webhookPayload.Repository.Id, webhookPayload.PullRequest.Head.Ref);

            await lifecycleStore.StoreEvent(new PullRequestEvent(
                repository: repository,
                username: username,
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
