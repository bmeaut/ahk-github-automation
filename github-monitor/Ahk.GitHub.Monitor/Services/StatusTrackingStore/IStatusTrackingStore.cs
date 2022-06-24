using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services
{
    public interface IStatusTrackingStore
    {
        Task StoreEvent(RepositoryCreateEvent repositoryCreateEvent);
        Task StoreEvent(BranchCreateEvent branchCreateEvent);
        Task StoreEvent(WorkflowRunEvent workflowRunEvent);
        Task StoreEvent(PullRequestEvent pullRequestEvent);
    }
}
