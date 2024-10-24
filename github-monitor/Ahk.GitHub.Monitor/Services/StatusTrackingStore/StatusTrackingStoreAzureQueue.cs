using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services.AzureQueues;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;

namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore;

internal class StatusTrackingStoreAzureQueue : IStatusTrackingStore
{
    private const string QueueNameRepositoryCreate = "ahkstatustracking-assignment-accepted";
    private const string QueueNamePullRequestOpened = "ahkstatustracking-pull-request-opened";
    private const string QueueNameTeacherAssigned = "ahkstatustracking-teacher-assigned";
    private const string QueueNamePullRequestStatusChanged = "ahkstatustracking-pull-request-status-changed";

    private readonly QueueWithCreateIfNotExists queueRepositoryCreate;
    private readonly QueueWithCreateIfNotExists queuePrOpened;
    private readonly QueueWithCreateIfNotExists queueTeacherAssigned;
    private readonly QueueWithCreateIfNotExists queuePullRequestStatusChanged;

    public StatusTrackingStoreAzureQueue(IAzureClientFactory<QueueServiceClient> clientFactory)
    {
        QueueServiceClient queueService = clientFactory.CreateClient(QueueClientName.Name);
        queueRepositoryCreate = new QueueWithCreateIfNotExists(queueService, QueueNameRepositoryCreate);
        queuePrOpened = new QueueWithCreateIfNotExists(queueService, QueueNamePullRequestOpened);
        queueTeacherAssigned = new QueueWithCreateIfNotExists(queueService, QueueNameTeacherAssigned);
        queuePullRequestStatusChanged =
            new QueueWithCreateIfNotExists(queueService, QueueNamePullRequestStatusChanged);
    }

    public Task StoreEvent(RepositoryCreateEvent repositoryCreateEvent) =>
        storeEvent(queueRepositoryCreate, repositoryCreateEvent);

    public Task StoreEvent(PullRequestOpenedEvent pullRequestOpenedEvent) =>
        storeEvent(queuePrOpened, pullRequestOpenedEvent);

    public Task StoreEvent(TeacherAssignedEvent teacherAssignedEvent) =>
        storeEvent(queueTeacherAssigned, teacherAssignedEvent);

    public Task StoreEvent(PullRequestStatusChanged pullRequestStatusChangedű) =>
        storeEvent(queuePullRequestStatusChanged, pullRequestStatusChangedű);

    private static Task storeEvent(QueueWithCreateIfNotExists queue, StatusEventBase @event) => queue.Send(@event);
}
