using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class BranchCreateStatusTrackingHandler : RepositoryEventBase<CreateEventPayload>
    {
        public const string GitHubWebhookEventName = "create";
        private readonly IStatusTrackingStore statusTrackingStore;

        public BranchCreateStatusTrackingHandler(IGitHubClientFactory gitHubClientFactory, IStatusTrackingStore statusTrackingStore, IMemoryCache cache, ILogger logger)
            : base(gitHubClientFactory, cache, logger)
        {
            this.statusTrackingStore = statusTrackingStore;
        }

        protected override async Task<EventHandlerResult> executeCore(CreateEventPayload webhookPayload)
        {
            if (webhookPayload.RefType.Equals(RefType.Branch))
            {
                // repository create is handled here; although it has a dedicated event, at tat time the repository is empty as has no ahk-monitor.yml, which is required
                // instead the main branch creation is used to recognize new repository creation
                if (webhookPayload.Ref.Equals(webhookPayload.Repository.DefaultBranch, StringComparison.OrdinalIgnoreCase))
                    return await processRepositoryCreateEvent(webhookPayload);
                else
                    return await processBranchCreateEvent(webhookPayload);
            }

            return EventHandlerResult.EventNotOfInterest($"branch create ignored for RefType: {webhookPayload.RefType}, Ref: {webhookPayload.Ref}");
        }

        private async Task<EventHandlerResult> processBranchCreateEvent(CreateEventPayload webhookPayload)
        {
            var repository = webhookPayload.Repository.FullName;
            var branch = webhookPayload.Ref;

            await statusTrackingStore.StoreEvent(new BranchCreateEvent(
                repository: repository,
                timestamp: DateTime.UtcNow,
                branch: branch));

            return EventHandlerResult.ActionPerformed("branch create lifecycle handled");
        }

        private async Task<EventHandlerResult> processRepositoryCreateEvent(CreateEventPayload webhookPayload)
        {
            var repository = webhookPayload.Repository.FullName;

            await statusTrackingStore.StoreEvent(new RepositoryCreateEvent(
                repository: repository,
                timestamp: DateTime.UtcNow));

            return EventHandlerResult.ActionPerformed("repository create lifecycle handled");
        }
    }
}
