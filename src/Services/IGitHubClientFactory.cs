using Octokit;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services
{
    public interface IGitHubClientFactory
    {
        Task<GitHubClient> CreateGitHubClient(long installationId);
    }
}