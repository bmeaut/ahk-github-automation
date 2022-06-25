using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class RepositoryCreateStatusTrackingHandler : RepositoryEventBase<RepositoryEventPayload>
    {
        public const string GitHubWebhookEventName = "repository";
        private readonly IStatusTrackingStore statusTrackingStore;

        public RepositoryCreateStatusTrackingHandler(IGitHubClientFactory gitHubClientFactory, IStatusTrackingStore statusTrackingStore, IMemoryCache cache, ILogger logger)
            : base(gitHubClientFactory, cache, logger)
        {
            this.statusTrackingStore = statusTrackingStore;
        }

        protected override async Task<EventHandlerResult> executeCore(RepositoryEventPayload webhookPayload)
        {
            if (webhookPayload.Action.Equals("created", StringComparison.OrdinalIgnoreCase))
                return await processRepositoryCreateEvent(webhookPayload);

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }

        private async Task<EventHandlerResult> processRepositoryCreateEvent(RepositoryEventPayload webhookPayload)
        {
            var repository = webhookPayload.Repository.FullName;

            await statusTrackingStore.StoreEvent(new RepositoryCreateEvent(
                repository: repository,
                timestamp: DateTime.UtcNow));

            return EventHandlerResult.ActionPerformed("repository create lifecycle handled");
        }
    }
}
