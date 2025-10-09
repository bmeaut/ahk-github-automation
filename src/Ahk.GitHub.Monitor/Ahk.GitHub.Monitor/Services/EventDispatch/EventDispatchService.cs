using Ahk.GitHub.Monitor.EventHandlers.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services.EventDispatch;

internal class EventDispatchService : IEventDispatchService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, List<Type>> _handlers;

    public EventDispatchService(IServiceProvider serviceProvider, EventDispatchConfig handlersConfig)
    {
        _serviceProvider = serviceProvider;

        var handlersList = new Dictionary<string, List<Type>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (gitHubEventName, handlerType) in handlersConfig.Handlers)
        {
            if (handlersList.TryGetValue(gitHubEventName, out var l))
            {
                l.Add(handlerType);
            }
            else
            {
                handlersList[gitHubEventName] = [handlerType];
            }
        }

        _handlers = handlersList;
    }

    public async Task ProcessAsync(string gitHubEventName, string requestBody, WebhookResult webhookResult, ILogger logger)
    {
        if (!_handlers.TryGetValue(gitHubEventName, out var handlersForEvent))
        {
            webhookResult.LogInfo($"Event {gitHubEventName} is not of interest");
            logger.LogInformation("Event {GitHubEventName} is not of interest", gitHubEventName);
        }
        else
        {
            foreach (var handlerType in handlersForEvent)
            {
                logger.LogInformation("Event {GitHubEventName} being handled by {HandlerType}", gitHubEventName, handlerType);
                await ExecuteHandlerAsync(requestBody, webhookResult, handlerType, logger);
            }
        }
    }

    private async Task ExecuteHandlerAsync(string requestBody, WebhookResult webhookResult, Type handlerType, ILogger logger)
    {
        try
        {
            var handler = ActivatorUtilities.CreateInstance(_serviceProvider, handlerType) as IGitHubEventHandler;
            var handlerResult = await handler.ExecuteAsync(requestBody);

            logger.LogInformation("{HandlerTypeName} result: {HandlerResult}", handlerType.Name, handlerResult.Result);
            webhookResult.LogInfo($"{handlerType.Name} -> {handlerResult.Result}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{HandlerTypeName} execution failed", handlerType.Name);
            webhookResult.LogError(ex, $"{handlerType.Name} -> exception");
        }
    }
}
