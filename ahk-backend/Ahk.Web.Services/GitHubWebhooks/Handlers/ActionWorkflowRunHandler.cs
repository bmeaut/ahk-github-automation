using Ahk.Web.Services.GitHubWebhooks.Payloads;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers;

/// <summary>
/// Warns a student who has run the automated evaluation more times than the course allows. Ported from
/// <c>github-monitor/.../EventHandlers/ActionWorkflowRunHandler.cs</c>.
///
/// The one behavioural change in the port: the threshold was a compile-time constant of 5, because each
/// deployment served exactly one course. It now comes from <c>CourseGitHubConfig.WorkflowRunThreshold</c>.
/// </summary>
public sealed class ActionWorkflowRunHandler : RepositoryEventHandlerBase<WorkflowRunEventPayload>
{
    private const string WarningText = ":exclamation: **You triggered too many automated evaluations; extra evaluations are penalized. Túl sok automata értékelést futtattál; az extra futtatások pontlevonással járnak.** ";

    public ActionWorkflowRunHandler(IMemoryCache cache, ILogger<ActionWorkflowRunHandler> logger)
        : base(cache, logger)
    {
    }

    public override string GitHubEventName => "workflow_run";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(GitHubWebhookContext context, WorkflowRunEventPayload payload, CancellationToken cancellationToken)
    {
        if (payload.Action is null || !payload.Action.Equals("completed", StringComparison.OrdinalIgnoreCase))
            return EventHandlerResult.EventNotOfInterest(payload.Action ?? string.Empty);

        if (string.IsNullOrEmpty(payload.Sender?.Login))
            return EventHandlerResult.PayloadError("missing actor user");

        if (await IsUserOrganizationMemberAsync(context, payload, payload.Sender.Login))
            return EventHandlerResult.NoActionNeeded("workflow_run ok, not triggered by student");

        var workflowRuns = await CountWorkflowRunsAsync(context, payload.Repository.Owner.Login, payload.Repository.Name, payload.Sender.Login);
        if (workflowRuns <= context.WorkflowRunThreshold)
            return EventHandlerResult.NoActionNeeded("workflow_run ok, has less then threshold");

        var prNum = await GetMostRecentPullRequestAsync(context, payload);
        if (prNum.HasValue)
            await context.GitHubClient.Issue.Comment.Create(payload.Repository.Id, prNum.Value, WarningText);

        return EventHandlerResult.ActionPerformed("workflow_run warning, threshold exceeded");
    }

    /// <summary>
    /// Kept as a raw <c>Connection</c> call rather than Octokit's <c>Actions.Workflows.Runs</c> client. What is
    /// wanted is GitHub's own <c>total_count</c> for the filtered query; Octokit's paginating client would
    /// count differently, and a silent change here changes a student's grade.
    /// </summary>
    private static async Task<int> CountWorkflowRunsAsync(GitHubWebhookContext context, string owner, string repo, string actor)
    {
        var response = await context.GitHubClient.Connection.Get<ListWorkflowRunsResponse>(
            uri: new Uri($"repos/{owner}/{repo}/actions/runs", UriKind.Relative),
            parameters: new Dictionary<string, string>
            {
                ["actor"] = actor,
                ["status"] = "completed",
            },
            accepts: AcceptHeaders.StableVersionJson);

        return response.Body.TotalCount;
    }

    private static async Task<int?> GetMostRecentPullRequestAsync(GitHubWebhookContext context, WorkflowRunEventPayload payload)
    {
        var list = await context.GitHubClient.PullRequest.GetAllForRepository(
            payload.Repository.Id, new PullRequestRequest { State = ItemStateFilter.All });

        return list.OrderByDescending(p => p.UpdatedAt).FirstOrDefault()?.Number;
    }

    internal sealed class ListWorkflowRunsResponse
    {
        public int TotalCount { get; set; }
    }
}
