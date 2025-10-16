using System;
using System.Linq;
using System.Threading.Tasks;

using Ahk.GitHub.Monitor.EventHandlers.Abstractions;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers;

public class PullRequestStatusTrackingHandler(
    IGitHubClientFactory gitHubClientFactory,
    IStatusTrackingStore statusTrackingStore,
    IMemoryCache cache,
    ILogger<PullRequestStatusTrackingHandler> logger)
    : RepositoryEventHandlerBase<PullRequestEventPayload>(gitHubClientFactory, cache, logger), IGitHubEventHandler
{
    public static string GitHubWebhookEventName => "pull_request";

    public async Task<EventHandlerResult> PrStatusChangedAsync(string gitHubRepositoryUrl, string pullRequestUrl, long pullRequestGitHubId, PullRequestStatus pullRequestStatus)
    {
        await statusTrackingStore.StoreEventAsync(new PullRequestStatusChanged()
        {
            GitHubRepositoryUrl = gitHubRepositoryUrl,
            PullRequestGitHubId = pullRequestGitHubId,
            PullRequestUrl = pullRequestUrl,
            PullRequestStatus = pullRequestStatus
        });

        return EventHandlerResult.ActionPerformed("pull request status changed lifecycle handled");
    }

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(PullRequestEventPayload webhookPayload)
    {
        if (webhookPayload.PullRequest == null)
        {
            return EventHandlerResult.PayloadError("no pull request information in webhook payload");
        }

        return webhookPayload.Action.ToLowerInvariant() switch
        {
            "opened" => await ProcessPullRequestOpenedEventAsync(webhookPayload),
            "assigned" or "unassigned" => await TeacherAssignedEventAsync(webhookPayload),
            "review_requested" => await TeacherAssignedAsReviewerEventAsync(webhookPayload),
            "closed" => await PrStatusChangedAsync(webhookPayload.Repository.HtmlUrl, webhookPayload.PullRequest.HtmlUrl, webhookPayload.PullRequest.Id, PullRequestStatus.Closed),
            _ => EventHandlerResult.EventNotOfInterest(webhookPayload.Action)
        };
    }

    private async Task<EventHandlerResult> ProcessPullRequestOpenedEventAsync(PullRequestEventPayload webhookPayload)
    {
        await statusTrackingStore.StoreEventAsync(new PullRequestOpenedEvent()
        {
            PullRequestGitHubId = webhookPayload.PullRequest.Id,
            BranchName = webhookPayload.PullRequest.Head.Ref,
            PullRequestUrl = webhookPayload.PullRequest.HtmlUrl,
            OpeningDate = DateTimeOffset.UtcNow,
            GitHubRepositoryUrl = webhookPayload.Repository.HtmlUrl
        });

        return EventHandlerResult.ActionPerformed("pull request opened lifecycle handled");
    }

    private async Task<EventHandlerResult> TeacherAssignedEventAsync(PullRequestEventPayload webhookPayload)
    {
        await statusTrackingStore.StoreEventAsync(new TeacherAssignedEvent()
        {
            PullRequestGitHubId = webhookPayload.PullRequest.Id,
            GitHubRepositoryUrl = webhookPayload.Repository.HtmlUrl,
            PullRequestUrl = webhookPayload.PullRequest.HtmlUrl,
            TeacherGitHubIds = webhookPayload.PullRequest.Assignees?.Select(u => u.Login)?.ToArray()
        });

        return EventHandlerResult.ActionPerformed("pull request assigned lifecycle handled");
    }

    private async Task<EventHandlerResult> TeacherAssignedAsReviewerEventAsync(PullRequestEventPayload webhookPayload)
    {
        await statusTrackingStore.StoreEventAsync(new TeacherAssignedEvent()
        {
            PullRequestGitHubId = webhookPayload.PullRequest.Id,
            GitHubRepositoryUrl = webhookPayload.Repository.HtmlUrl,
            PullRequestUrl = webhookPayload.PullRequest.HtmlUrl,
            TeacherGitHubIds = webhookPayload.PullRequest.RequestedReviewers?.Select(u => u.Login)?.ToArray()
        });

        return EventHandlerResult.ActionPerformed("pull request assigned as reviewer lifecycle handled");
    }
}
