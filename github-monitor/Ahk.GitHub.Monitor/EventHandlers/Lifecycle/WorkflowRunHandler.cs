using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class WorkflowRunHandler : RepositoryEventBase<WorkflowEventPayload>
    {
        public const string GitHubWebhookEventName = "workflow_run";
        private readonly ILifecycleStore lifecycleStore;

        public WorkflowRunHandler(IGitHubClientFactory gitHubClientFactory, ILifecycleStore lifecycleStore, IMemoryCache cache, ILogger logger)
            : base(gitHubClientFactory, cache, logger)
        {
            this.lifecycleStore = lifecycleStore;
        }

        protected override async Task<EventHandlerResult> executeCore(WorkflowEventPayload webhookPayload)
        {
            if (webhookPayload.WorkflowRun == null)
                return EventHandlerResult.PayloadError("no workflow run information in webhook payload");

            if (webhookPayload.Action.Equals("completed", StringComparison.OrdinalIgnoreCase))
                return await processWorkflowRunEvent(webhookPayload);

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }

        private async Task<EventHandlerResult> processWorkflowRunEvent(WorkflowEventPayload webhookPayload)
        {
            var repository = webhookPayload.Repository.FullName;
            var username = getGitHubUserNameFromRepositoryName(webhookPayload.Repository.FullName);
            var conclusion = webhookPayload.WorkflowRun.Conclusion;

            await lifecycleStore.StoreEvent(new WorkflowRunEvent(
                repository: repository,
                username: username,
                timestamp: DateTime.UtcNow,
                conclusion: conclusion));

            return EventHandlerResult.ActionPerformed("workflow_run lifecycle handled");
        }
    }
}
