using Ahk.GitHub.Monitor.EventHandlers.Abstractions;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers;

public class RepositoryCreatedStatusTrackingHandler(
    IGitHubClientFactory gitHubClientFactory,
    IStatusTrackingStore statusTrackingStore,
    IMemoryCache cache,
    ILogger<RepositoryCreatedStatusTrackingHandler> logger)
    : RepositoryEventHandlerBase<CreateEventPayload>(gitHubClientFactory, cache, logger), IGitHubEventHandler
{
    public static string GitHubWebhookEventName => "repository";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(CreateEventPayload webhookPayload)
    {
        await statusTrackingStore.StoreEventAsync(new RepositoryCreatedEvent() { GitHubRepositoryUrl = webhookPayload.Repository.HtmlUrl });
        return EventHandlerResult.ActionPerformed("repository created lifecycle handled");
    }
}
