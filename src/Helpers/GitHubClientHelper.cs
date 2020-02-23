using Octokit;
using Octokit.Internal;
using System;

namespace Ahk.GitHub.Monitor
{
    public static class GitHubClientHelper
    {
        public static GitHubClient CreateGitHubClient(string token)
        {
            var gitHubClient = new GitHubClient(new ProductHeaderValue("Ahk"), new InMemoryCredentialStore(new Credentials(token)));
            gitHubClient.SetRequestTimeout(TimeSpan.FromSeconds(5));
            return gitHubClient;
        }
    }
}
