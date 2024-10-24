using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers;

public class ActionWorkflowRunHandler(
    IGitHubClientFactory gitHubClientFactory,
    IMemoryCache cache,
    IServiceProvider serviceProvider)
    : RepositoryEventBase<WorkflowRunEventPayload>(gitHubClientFactory, cache, serviceProvider)
{
    public const int WorkflowRunThreshold = 5;
    public const string GitHubWebhookEventName = "workflow_run";

    private const string WarningText =
        ":exclamation: **You triggered too many automated evaluations; extra evaluations are penalized. Túl sok automata értékelést futtattál; az extra futtatások pontlevonással járnak.** ";

    protected override async Task<EventHandlerResult> executeCore(WorkflowRunEventPayload webhookPayload)
    {
        if (webhookPayload.Action.Equals("completed", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(webhookPayload.Sender?.Login))
            {
                return EventHandlerResult.PayloadError("missing actor user");
            }

            if (await this.isUserOrganizationMember(webhookPayload, webhookPayload.Sender.Login))
            {
                return EventHandlerResult.NoActionNeeded("workflow_run ok, not triggered by student");
            }

            var workflowRuns = await this.GitHubClient.CountWorkflowRunsInRepository(
                webhookPayload.Repository.Owner.Login, webhookPayload.Repository.Name, webhookPayload.Sender.Login);
            if (workflowRuns <= WorkflowRunThreshold)
            {
                return EventHandlerResult.NoActionNeeded("workflow_run ok, has less then threshold");
            }

            var prNum = await this.getMostRecentPullRequest(webhookPayload);
            if (prNum.HasValue)
            {
                await this.GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, prNum.Value, WarningText);
            }

            return EventHandlerResult.ActionPerformed("workflow_run warning, threshold exceeded");
        }

        return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
    }

    private async Task<int?> getMostRecentPullRequest(WorkflowRunEventPayload webhookPayload)
    {
        IReadOnlyList<PullRequest> list = await this.GitHubClient.PullRequest.GetAllForRepository(
            webhookPayload.Repository.Id,
            new PullRequestRequest() { State = ItemStateFilter.All });
        return list.OrderByDescending(p => p.UpdatedAt).FirstOrDefault()?.Number;
    }
}
