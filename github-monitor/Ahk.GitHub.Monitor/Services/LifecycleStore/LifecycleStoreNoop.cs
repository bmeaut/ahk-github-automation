using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services
{
    public class LifecycleStoreNoop : ILifecycleStore
    {
        public Task StoreEvent(RepositoryCreateEvent repositoryCreateEvent) => Task.CompletedTask;
        public Task StoreEvent(BranchCreateEvent branchCreateEvent) => Task.CompletedTask;
        public Task StoreEvent(WorkflowRunEvent workflowRunEvent) => Task.CompletedTask;
        public Task StoreEvent(PullRequestEvent pullRequestEvent) => Task.CompletedTask;
    }
}
