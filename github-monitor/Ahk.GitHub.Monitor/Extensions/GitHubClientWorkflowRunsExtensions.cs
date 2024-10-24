using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octokit
{
    internal static class GitHubClientWorkflowRunsExtensions
    {
        public static async Task<int> CountWorkflowRunsInRepository(this IGitHubClient client, string owner,
            string repo, string actor)
        {
            var r = await client.Connection.Get<ListWorkflowRunsResponse>(
                uri: new Uri($"repos/{owner}/{repo}/actions/runs", UriKind.Relative),
                parameters: new Dictionary<string, string>() { ["actor"] = actor, ["status"] = "completed", },
                accepts: AcceptHeaders.StableVersionJson);
            return r.Body.TotalCount;
        }

        public class ListWorkflowRunsResponse
        {
            public int TotalCount { get; set; }
        }
    }
}
