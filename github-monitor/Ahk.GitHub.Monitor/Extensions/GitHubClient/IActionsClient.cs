using System.Threading.Tasks;

namespace Octokit
{
    public interface IActionsClient
    {
        Task<int> CountWorkflowRunsInRepository(string owner, string repo, string actor);
        Task DisableActionsForRepository(string owner, string repo);
    }
}
