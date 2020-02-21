using Octokit;
using System;
using System.Linq;

namespace Ahk.GitHub.Monitor
{
    public static class RepositoryFilterHelper
    {
        public static bool IsRepositoryOfInterrest(Repository repository)
        {
            var repositoryPrefixes = Environment.GetEnvironmentVariable("AHK_GITHUB_REPOSITORY_PREFIXES", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(repositoryPrefixes))
                return true;
            return repositoryPrefixes.Split(';').Any(prefix => repository.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
