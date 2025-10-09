using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore;

public interface IStatusTrackingStore
{
    public Task StoreEventAsync(RepositoryCreatedEvent repositoryCreateEvent);
    public Task StoreEventAsync(PullRequestOpenedEvent pullRequestOpenedEvent);
    public Task StoreEventAsync(TeacherAssignedEvent teacherAssignedEvent);
    public Task StoreEventAsync(PullRequestStatusChanged pullRequestStatusChanged);
}
