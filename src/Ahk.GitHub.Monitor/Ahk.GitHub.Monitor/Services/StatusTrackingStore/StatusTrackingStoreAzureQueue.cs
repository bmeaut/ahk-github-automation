using Ahk.GitHub.Monitor.Services.AzureQueues;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

using Azure.Storage.Queues;

using Microsoft.Extensions.Azure;

using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore;

internal class StatusTrackingStoreAzureQueue : IStatusTrackingStore
{
    private const string QueueNameRepositoryCreate = "ahkstatustracking-assignment-accepted";
    private const string QueueNamePullRequestOpened = "ahkstatustracking-pull-request-opened";
    private const string QueueNameTeacherAssigned = "ahkstatustracking-teacher-assigned";
    private const string QueueNamePullRequestStatusChanged = "ahkstatustracking-pull-request-status-changed";

    private readonly QueueWithCreateIfNotExists _queueRepositoryCreate;
    private readonly QueueWithCreateIfNotExists _queuePrOpened;
    private readonly QueueWithCreateIfNotExists _queueTeacherAssigned;
    private readonly QueueWithCreateIfNotExists _queuePullRequestStatusChanged;

    public StatusTrackingStoreAzureQueue(IAzureClientFactory<QueueServiceClient> clientFactory)
    {
        var queueService = clientFactory.CreateClient(QueueClientName.Name);

        _queueRepositoryCreate = new QueueWithCreateIfNotExists(queueService, QueueNameRepositoryCreate);
        _queuePrOpened = new QueueWithCreateIfNotExists(queueService, QueueNamePullRequestOpened);
        _queueTeacherAssigned = new QueueWithCreateIfNotExists(queueService, QueueNameTeacherAssigned);
        _queuePullRequestStatusChanged = new QueueWithCreateIfNotExists(queueService, QueueNamePullRequestStatusChanged);
    }

    public Task StoreEventAsync(RepositoryCreatedEvent repositoryCreateEvent) =>
        _queueRepositoryCreate.Send(repositoryCreateEvent);

    public Task StoreEventAsync(PullRequestOpenedEvent pullRequestOpenedEvent) =>
        _queuePrOpened.Send(pullRequestOpenedEvent);

    public Task StoreEventAsync(TeacherAssignedEvent teacherAssignedEvent) =>
        _queueTeacherAssigned.Send(teacherAssignedEvent);

    public Task StoreEventAsync(PullRequestStatusChanged pullRequestStatusChanged) =>
        _queuePullRequestStatusChanged.Send(pullRequestStatusChanged);
}
