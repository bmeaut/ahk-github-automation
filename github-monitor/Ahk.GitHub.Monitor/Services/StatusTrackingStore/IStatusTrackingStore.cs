using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore;

public interface IStatusTrackingStore
{
    Task StoreEvent(RepositoryCreateEvent repositoryCreateEvent);
    Task StoreEvent(PullRequestOpenedEvent pullRequestOpenedEvent);
    Task StoreEvent(TeacherAssignedEvent teacherAssignedEvent);
    Task StoreEvent(PullRequestStatusChanged pullRequestStatusChanged);
}
