using Octokit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class ActionWorkflowRunHandler : RepositoryEventBase<WorkflowRunEventPayload>
    {
        public const string GitHubWebhookEventName = "workflow_run";
        private const string WarningText = ":exclamation: **You triggered too many automated evaluations; you have one more, after which it will be disabled. Túl sok automata értékelést futtattál; még egyet lehetőséged van, utána kikapcsoljuk.** ";
        private const string DisabledText = ":disappointed: **Automated evaluations have been disabled. Az automata értékelések ki lettek kapcsolva.** ";

        public ActionWorkflowRunHandler(Services.IGitHubClientFactory gitHubClientFactory, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
            : base(gitHubClientFactory, cache)
        {
        }

        protected override async Task<EventHandlerResult> executeCore(WorkflowRunEventPayload webhookPayload)
        {
            if (webhookPayload.Action.Equals("completed", StringComparison.OrdinalIgnoreCase))
            {
                if (await isUserOrganizationMember(webhookPayload, webhookPayload.Sender.Login))
                    return EventHandlerResult.NoActionNeeded("workflow_run ok, not triggered by student");

                var workflowRuns = await GitHubClient.CountWorkflowRunsInRepository(webhookPayload.Repository.Owner.Login, webhookPayload.Repository.Name, webhookPayload.Sender.Login);
                if (workflowRuns <= 5)
                    return EventHandlerResult.NoActionNeeded("workflow_run ok, has less then threshold");

                var prNum = await getMostRecentPullRequest(webhookPayload);
                if (workflowRuns <= 7)
                {
                    if (prNum.HasValue)
                        await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, prNum.Value, WarningText);
                    return EventHandlerResult.ActionPerformed("workflow_run warning, threshold exceeded");
                }
                else
                {
                    if (prNum.HasValue)
                        await GitHubClient.Issue.Comment.Create(webhookPayload.Repository.Id, prNum.Value, DisabledText);
                    await GitHubClient.DisableActionsForRepository(webhookPayload.Repository.Owner.Login, webhookPayload.Repository.Name);
                    return EventHandlerResult.ActionPerformed("workflow_run limit exceeded, actions disabled");
                }
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
