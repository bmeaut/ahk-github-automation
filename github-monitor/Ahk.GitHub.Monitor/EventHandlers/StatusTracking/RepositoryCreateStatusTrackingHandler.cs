using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers.StatusTracking;

public class RepositoryCreateStatusTrackingHandler(
    IGitHubClientFactory gitHubClientFactory,
    IStatusTrackingStore statusTrackingStore,
    IMemoryCache cache,
    IServiceProvider serviceProvider)
    : RepositoryEventBase<CreateEventPayload>(gitHubClientFactory, cache, serviceProvider)
{
    public const string GitHubWebhookEventName = "repository";

    protected override async Task<EventHandlerResult> executeCore(CreateEventPayload webhookPayload) =>
        await this.processRepositoryCreateEvent(webhookPayload);


    private async Task<EventHandlerResult> processRepositoryCreateEvent(CreateEventPayload webhookPayload)
    {
        var repositoryUrl = webhookPayload.Repository.HtmlUrl;

        await statusTrackingStore.StoreEvent(new RepositoryCreateEvent(
            repositoryUrl));

        return EventHandlerResult.ActionPerformed("repository create lifecycle handled");
    }
}
