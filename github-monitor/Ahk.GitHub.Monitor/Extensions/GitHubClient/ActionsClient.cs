using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octokit
{
    internal class ActionsClient : IActionsClient
    {
        private readonly IGitHubClientEx client;

        public ActionsClient(IGitHubClientEx client)
            => this.client = client;

        public async Task<int> CountWorkflowRunsInRepository(string owner, string repo, string actor)
        {
            var r = await client.Connection.Get<ListWorkflowRunsResponse>(
                uri: new Uri($"repos/{owner}/{repo}/actions/runs", UriKind.Relative),
                parameters: new Dictionary<string, string>()
                {
                    ["actor"] = actor,
                    ["status"] = "completed",
                },
                accepts: AcceptHeaders.StableVersionJson);
            return r.Body.TotalCount;
        }

        public Task DisableActionsForRepository(string owner, string repo)
            => client.Connection.Put<object>(
                uri: new Uri($"/repos/{owner}/{repo}/actions/permissions", UriKind.Relative),
                body: new SetActionsPermissionsRequest() { Enabled = false });

        public class ListWorkflowRunsResponse
        {
            public int TotalCount { get; set; }
        }

        public class SetActionsPermissionsRequest
        {
            public bool Enabled { get; set; }
        }
    }
}
