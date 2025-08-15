using System;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers.BaseAndUtils;
using Ahk.GitHub.Monitor.Services;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;
using Microsoft.Extensions.Caching.Memory;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers;

public class PullRequestStatusTrackingHandler(
    IGitHubClientFactory gitHubClientFactory,
    IStatusTrackingStore statusTrackingStore,
    IMemoryCache cache,
    IServiceProvider serviceProvider)
    : RepositoryEventBase<PullRequestEventPayload>(gitHubClientFactory, cache, serviceProvider)
{
    public const string GitHubWebhookEventName = "pull_request";

    public async Task<EventHandlerResult> PrStatusChanged(string gitHubRepositoryUrl, string pullRequestUrl,
        PullRequestStatus pullRequestStatus)
    {
        await statusTrackingStore.StoreEvent(new PullRequestStatusChanged(
            gitHubRepositoryUrl,
            pullRequestUrl,
            pullRequestStatus));

        return EventHandlerResult.ActionPerformed("pull request status changed lifecycle handled");
    }

    protected override async Task<EventHandlerResult> executeCore(PullRequestEventPayload webhookPayload)
    {
        if (webhookPayload.PullRequest == null)
        {
            return EventHandlerResult.PayloadError("no pull request information in webhook payload");
        }

        if (webhookPayload.Action.Equals("opened", StringComparison.OrdinalIgnoreCase))
        {
            return await this.processPullRequestOpenedEvent(webhookPayload);
        }

        if (webhookPayload.Action.Equals("assigned", StringComparison.OrdinalIgnoreCase) ||
            webhookPayload.Action.Equals("unassigned", StringComparison.OrdinalIgnoreCase))
        {
            return await this.teacherAssignedEvent(webhookPayload);
        }

        if (webhookPayload.Action.Equals("review_requested", StringComparison.OrdinalIgnoreCase))
        {
            return await this.teacherAssignedAsReviewerEvent(webhookPayload);
        }

        if (webhookPayload.Action.Equals("closed", StringComparison.OrdinalIgnoreCase))
        {
            return await this.PrStatusChanged(webhookPayload.Repository.HtmlUrl, webhookPayload.PullRequest.HtmlUrl,
                PullRequestStatus.Closed);
        }

        return EventHandlerResult.EventNotOfInterest(webhookPayload.Action);
    }

    private async Task<EventHandlerResult> processPullRequestOpenedEvent(PullRequestEventPayload webhookPayload)
    {
        await statusTrackingStore.StoreEvent(new PullRequestOpenedEvent(
            webhookPayload.Repository.HtmlUrl,
            DateTimeOffset.UtcNow,
            webhookPayload.PullRequest.Head.Ref,
            webhookPayload.PullRequest.HtmlUrl));

        return EventHandlerResult.ActionPerformed("pull request opened lifecycle handled");
    }

    private async Task<EventHandlerResult> teacherAssignedEvent(PullRequestEventPayload webhookPayload)
    {
        var assignees = webhookPayload.PullRequest.Assignees?.Select(u => u.Login)?.ToArray();

        await statusTrackingStore.StoreEvent(new TeacherAssignedEvent(
            webhookPayload.Repository.HtmlUrl,
            webhookPayload.PullRequest.HtmlUrl,
            assignees));

        return EventHandlerResult.ActionPerformed("pull request assigned lifecycle handled");
    }

    private async Task<EventHandlerResult> teacherAssignedAsReviewerEvent(PullRequestEventPayload webhookPayload)
    {
        var assignees = webhookPayload.PullRequest.RequestedReviewers?.Select(u => u.Login)?.ToArray();

        await statusTrackingStore.StoreEvent(new TeacherAssignedEvent(
            webhookPayload.Repository.HtmlUrl,
            webhookPayload.PullRequest.HtmlUrl,
            assignees));

        return EventHandlerResult.ActionPerformed("pull request assigned as reviewer lifecycle handled");
    }
}
