using Octokit;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Extensions;

internal static class GitHubClientWorkflowRunsExtensions
{
    public static async Task<int> CountWorkflowRunsInRepositoryAsync(this IGitHubClient client, string owner, string repo, string actor)
    {
        var r = await client.Connection.Get<ListWorkflowRunsResponse>(
            new Uri($"repos/{owner}/{repo}/actions/runs", UriKind.Relative),
            new Dictionary<string, string>() { ["actor"] = actor, ["status"] = "completed" },
            AcceptHeaders.StableVersionJson);
        return r.Body.TotalCount;
    }

    public class ListWorkflowRunsResponse
    {
        public int TotalCount { get; set; }
    }
}
