using Octokit;
using Octokit.Internal;
using System;

namespace Ahk.GitHub.Monitor
{
    public static class GitHubClientHelper
    {
        public static bool TryCreateGitHubClient(out GitHubClient gitHubClient)
        {
            var githubToken = Environment.GetEnvironmentVariable("AHK_GITHUB_TOKEN", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(githubToken) || string.IsNullOrWhiteSpace(githubToken))
            {
                gitHubClient = null;
                return false;
            }

            gitHubClient = new GitHubClient(new ProductHeaderValue("Ahk"), new InMemoryCredentialStore(new Credentials(githubToken)));
            gitHubClient.SetRequestTimeout(TimeSpan.FromSeconds(5));
            return true;
        }
    }
}
