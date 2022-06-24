using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services.AzureQueues;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;

namespace Ahk.GitHub.Monitor.Services
{
    internal class StatusTrackingStoreAzureQueue : IStatusTrackingStore
    {
        public const string QueueNameRepositoryCreate = "ahkstatustrackingrepocreate";
        public const string QueueNameBranchCreate = "ahkstatustrackingbranchcreate";
        public const string QueueNameWorkflowRun = "ahkstatustrackingworkflowrun";
        public const string QueueNamePullRequest = "ahkstatustrackingpullrequest";

        private readonly QueueWithCreateIfNotExists queueRepositoryCreate;
        private readonly QueueWithCreateIfNotExists queueBranchCreate;
        private readonly QueueWithCreateIfNotExists queueWorkflowRun;
        private readonly QueueWithCreateIfNotExists queuePullRequest;

        public StatusTrackingStoreAzureQueue(IAzureClientFactory<QueueServiceClient> clientFactory)
        {
            var queueService = clientFactory.CreateClient(QueueClientName.Name);
            queueRepositoryCreate = new QueueWithCreateIfNotExists(queueService, QueueNameRepositoryCreate);
            queueBranchCreate = new QueueWithCreateIfNotExists(queueService, QueueNameBranchCreate);
            queueWorkflowRun = new QueueWithCreateIfNotExists(queueService, QueueNameWorkflowRun);
            queuePullRequest = new QueueWithCreateIfNotExists(queueService, QueueNamePullRequest);
        }

        public Task StoreEvent(RepositoryCreateEvent repositoryCreateEvent) => storeEvent(queueRepositoryCreate, repositoryCreateEvent);
        public Task StoreEvent(BranchCreateEvent branchCreateEvent) => storeEvent(queueBranchCreate, branchCreateEvent);
        public Task StoreEvent(WorkflowRunEvent workflowRunEvent) => storeEvent(queueWorkflowRun, workflowRunEvent);
        public Task StoreEvent(PullRequestEvent pullRequestEvent) => storeEvent(queuePullRequest, pullRequestEvent);
        private static Task storeEvent(QueueWithCreateIfNotExists queue, StatusEventBase @event) => queue.Send(@event);
    }
}
