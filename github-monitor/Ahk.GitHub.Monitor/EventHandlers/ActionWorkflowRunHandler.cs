using System;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class ActionWorkflowRunHandler(
        Services.IGitHubClientFactory gitHubClientFactory,
        Microsoft.Extensions.Caching.Memory.IMemoryCache cache,
        Microsoft.Extensions.Logging.ILogger logger)
        : RepositoryEventBase<WorkflowRunEventPayload>(gitHubClientFactory, cache, logger)
    {
        public const int WorkflowRunThreshold = 5;
        public const string GitHubWebhookEventName = "workflow_run";
        private const string WarningText = ":exclamation: **You triggered too many automated evaluations; extra evaluations are penalized. Túl sok automata értékelést futtattál; az extra futtatások pontlevonással járnak.** ";

        protected override async Task<EventHandlerResult> executeCore(WorkflowRunEventPayload webhookPayload)
        {
            if (webhookPayload.Action.Equals("completed", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(webhookPayload.Sender?.Login))
                    return EventHandlerResult.PayloadError("missing actor user");

                if (await isUserOrganizationMember(webhookPayload, webhookPayload.Sender.Login))
                    return EventHandlerResult.NoActionNeeded("workflow_run ok, not triggered by student");

                var workflowRuns = await GitHubClient.CountWorkflowRunsInRepository(webhookPayload.Repository.Owner.Login, webhookPayload.Repository.Name, webhookPayload.Sender.Login);
                if (workflowRuns <= WorkflowRunThreshold)
                    return EventHandlerResult.NoActionNeeded("workflow_run ok, has less then threshold");

                var prNum = await getMostRecentPullRequest(webhookPayload);
                if (prNum.HasValue)
                    await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, prNum.Value, WarningText);
                return EventHandlerResult.ActionPerformed("workflow_run warning, threshold exceeded");
            }

            return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
        }

        private async Task<int?> getMostRecentPullRequest(WorkflowRunEventPayload webhookPayload)
        {
            var list = await GitHubClient.PullRequest.GetAllForRepository(webhookPayload.Repository.Id, new PullRequestRequest() { State = ItemStateFilter.All });
            return list.OrderByDescending(p => p.UpdatedAt).FirstOrDefault()?.Number;
        }
    }
}
