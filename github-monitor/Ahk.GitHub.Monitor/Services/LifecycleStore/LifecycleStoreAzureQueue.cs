using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;

namespace Ahk.GitHub.Monitor.Services
{
    internal class LifecycleStoreAzureQueue : ILifecycleStore
    {
        public const string QueueClientName = "ahkevents";
        public const string QueueNameRepositoryCreate = "ahk-repository-create";
        public const string QueueNameBranchCreate = "ahk-branch-create";
        public const string QueueNameWorkflowRun = "ahk-workflow-run";
        public const string QueueNamePullRequest = "ahk-pull-request";

        private readonly QueueWithCreateIfNotExists queueRepositoryCreate;
        private readonly QueueWithCreateIfNotExists queueBranchCreate;
        private readonly QueueWithCreateIfNotExists queueWorkflowRun;
        private readonly QueueWithCreateIfNotExists queuePullRequest;

        public LifecycleStoreAzureQueue(IAzureClientFactory<QueueServiceClient> clientFactory)
        {
            var queueService = clientFactory.CreateClient(QueueClientName);
            queueRepositoryCreate = new QueueWithCreateIfNotExists(queueService, QueueNameRepositoryCreate);
            queueBranchCreate = new QueueWithCreateIfNotExists(queueService, QueueNameBranchCreate);
            queueWorkflowRun = new QueueWithCreateIfNotExists(queueService, QueueNameWorkflowRun);
            queuePullRequest = new QueueWithCreateIfNotExists(queueService, QueueNamePullRequest);
        }

        public Task StoreEvent(RepositoryCreateEvent repositoryCreateEvent) => storeEvent(queueRepositoryCreate, repositoryCreateEvent);
        public Task StoreEvent(BranchCreateEvent branchCreateEvent) => storeEvent(queueBranchCreate, branchCreateEvent);
        public Task StoreEvent(WorkflowRunEvent workflowRunEvent) => storeEvent(queueWorkflowRun, workflowRunEvent);
        public Task StoreEvent(PullRequestEvent pullRequestEvent) => storeEvent(queuePullRequest, pullRequestEvent);
        private static Task storeEvent(QueueWithCreateIfNotExists queue, LifecycleEvent lifecycleEvent) => queue.Send(lifecycleEvent);
    }
}
