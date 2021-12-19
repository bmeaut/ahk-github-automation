using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.Services
{
    public interface IGitHubClientFactory
    {
        Task<IGitHubClient> CreateGitHubClient(long installationId, ILogger logger);
    }
}
