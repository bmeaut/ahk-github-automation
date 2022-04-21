using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class RepositoryCreateHandler : RepositoryEventBase<RepositoryEventPayload>
    {
        public const string GitHubWebhookEventName = "repository";
        private readonly ILifecycleStore lifecycleStore;

        public RepositoryCreateHandler(IGitHubClientFactory gitHubClientFactory, ILifecycleStore lifecycleStore, IMemoryCache cache, ILogger logger)
            : base(gitHubClientFactory, cache, logger)
        {
            this.lifecycleStore = lifecycleStore;
        }

        protected override async Task<EventHandlerResult> executeCore(RepositoryEventPayload webhookPayload)
        {
            if (webhookPayload.Repository == null)
                return EventHandlerResult.PayloadError("no repository information in webhook payload");

            if (webhookPayload.Action.Equals("created", StringComparison.OrdinalIgnoreCase))
                return await processRepositoryCreateEvent(webhookPayload);

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }

        private async Task<EventHandlerResult> processRepositoryCreateEvent(RepositoryEventPayload webhookPayload)
        {
            string repository = webhookPayload.Repository.FullName;
            string username = webhookPayload.Repository.FullName.Split("-")[^1];

            await lifecycleStore.StoreEvent(new RepositoryCreateEvent(
                repository: repository,
                username: username,
                timestamp: DateTime.UtcNow));

            return EventHandlerResult.ActionPerformed("repository create operation done");
        }
    }
}
