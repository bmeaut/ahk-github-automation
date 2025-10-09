using Microsoft.Extensions.Logging;

using Octokit;

using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services.GitHubClientFactory;

public interface IGitHubClientFactory
{
    public Task<IGitHubClient> CreateGitHubClientAsync(string gitHubOrg, long installationId, ILogger logger);
}
