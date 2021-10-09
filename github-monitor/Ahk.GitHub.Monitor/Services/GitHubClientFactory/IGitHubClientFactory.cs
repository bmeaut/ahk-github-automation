using Octokit;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services
{
    public interface IGitHubClientFactory
    {
        Task<IGitHubClientEx> CreateGitHubClient(long installationId);
    }
}