using System.Threading.Tasks;
using Octokit;

namespace Ahk.GitHub.Monitor.Services
{
    public interface IGitHubClientFactory
    {
        Task<IGitHubClient> CreateGitHubClient(long installationId);
    }
}