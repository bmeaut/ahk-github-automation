using Ahk.GitHub.Monitor.EventHandlers.Abstractions;
using Ahk.GitHub.Monitor.Extensions;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers;

public class ActionWorkflowRunHandler(
    IGitHubClientFactory gitHubClientFactory,
    IMemoryCache cache,
    ILogger<ActionWorkflowRunHandler> logger)
    : RepositoryEventHandlerBase<WorkflowRunEventPayload>(gitHubClientFactory, cache, logger), IGitHubEventHandler
{
    public static string GitHubWebhookEventName => "workflow_run";

    // TODO config legyen
    public const int WorkflowRunThreshold = 5;
    private const string WarningText = ":exclamation: **You triggered too many automated evaluations; extra evaluations are penalized. Túl sok automata értékelést futtattál; az extra futtatások pontlevonással járnak.** ";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(WorkflowRunEventPayload webhookPayload)
    {
        if (webhookPayload.Action.Equals("completed", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(webhookPayload.Sender?.Login))
            {
                return EventHandlerResult.PayloadError("missing actor user");
            }

            if (await GetIsUserOrganizationMemberAsync(webhookPayload, webhookPayload.Sender.Login))
            {
                return EventHandlerResult.NoActionNeeded("workflow_run ok, not triggered by student");
            }

            var workflowRuns = await GitHubClient.CountWorkflowRunsInRepositoryAsync(
                webhookPayload.Repository.Owner.Login,
                webhookPayload.Repository.Name,
                webhookPayload.Sender.Login);
            if (workflowRuns <= WorkflowRunThreshold)
            {
                return EventHandlerResult.NoActionNeeded("workflow_run ok, has less then threshold");
            }

            var prNum = await GetMostRecentPullRequestAsync(webhookPayload);
            if (prNum.HasValue)
            {
                await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, prNum.Value, WarningText);
            }

            return EventHandlerResult.ActionPerformed("workflow_run warning, threshold exceeded");
        }

        return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
    }

    private async Task<int?> GetMostRecentPullRequestAsync(WorkflowRunEventPayload webhookPayload)
    {
        var list = await GitHubClient.PullRequest.GetAllForRepository(
            webhookPayload.Repository.Id,
            new PullRequestRequest() { State = ItemStateFilter.All });

        return list.OrderByDescending(p => p.UpdatedAt)
            .FirstOrDefault()
            ?.Number;
    }
}
