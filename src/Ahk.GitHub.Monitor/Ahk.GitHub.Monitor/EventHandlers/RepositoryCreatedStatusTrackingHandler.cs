using Ahk.GitHub.Monitor.EventHandlers.Abstractions;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;
using Ahk.GradeManagement.Events;

using MassTransit;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers;

public class RepositoryCreatedStatusTrackingHandler(
    IGitHubClientFactory gitHubClientFactory,
    IPublishEndpoint publishEndpoint,
    IMemoryCache cache,
    ILogger<RepositoryCreatedStatusTrackingHandler> logger)
    : RepositoryEventHandlerBase<CreateEventPayload>(gitHubClientFactory, cache, logger), IGitHubEventHandler
{
    public static string GitHubWebhookEventName => "repository";

    protected override async Task<EventHandlerResult> ExecuteCoreAsync(CreateEventPayload webhookPayload)
    {
        await publishEndpoint.Publish(new AssignmentAccepted { GitHubRepositoryUrl = webhookPayload.Repository.HtmlUrl });
        return EventHandlerResult.ActionPerformed("repository created lifecycle handled");
    }
}
