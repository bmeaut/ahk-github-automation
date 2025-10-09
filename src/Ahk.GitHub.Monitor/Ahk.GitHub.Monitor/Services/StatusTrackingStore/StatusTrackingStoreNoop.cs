using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore;

public class StatusTrackingStoreNoop : IStatusTrackingStore
{
    public Task StoreEventAsync(RepositoryCreatedEvent repositoryCreateEvent) => Task.CompletedTask;
    public Task StoreEventAsync(PullRequestOpenedEvent pullRequestOpenedEvent) => Task.CompletedTask;
    public Task StoreEventAsync(TeacherAssignedEvent teacherAssignedEvent) => Task.CompletedTask;
    public Task StoreEventAsync(PullRequestStatusChanged pullRequestStatusChanged) => Task.CompletedTask;
}
