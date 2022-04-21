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
            if (webhookPayload.Repository == null)
                return EventHandlerResult.PayloadError("no repository information in webhook payload");

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
            string repository = webhookPayload.Repository.FullName;
            string username = webhookPayload.Repository.FullName.Split("-")[^1];
            string action = webhookPayload.Action;
            string[] assignees = webhookPayload.PullRequest.Assignees?.Select(u => u.Login)?.ToArray();
            string neptun = await getNeptun(webhookPayload.Repository.Id, webhookPayload.PullRequest.Head.Ref);

            await lifecycleStore.StoreEvent(new PullRequestEvent(
                repository: repository,
                username: username,
                timestamp: DateTime.UtcNow,
                action: action,
                assignees: assignees,
                neptun: neptun));

            return EventHandlerResult.ActionPerformed("pull request operation done");
        }
    }
}
