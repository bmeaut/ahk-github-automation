using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

namespace Ahk.GitHub.Monitor.Services
{
    public class StatusTrackingStoreNoop : IStatusTrackingStore
    {
        public Task StoreEvent(RepositoryCreateEvent repositoryCreateEvent) => Task.CompletedTask;
        public Task StoreEvent(PullRequestOpenedEvent pullRequestOpenedEvent) => Task.CompletedTask;
        public Task StoreEvent(TeacherAssignedEvent teacherAssignedEvent) => Task.CompletedTask;
        public Task StoreEvent(PullRequestStatusChanged pullRequestStatusChangedű) => Task.CompletedTask;
    }
}
