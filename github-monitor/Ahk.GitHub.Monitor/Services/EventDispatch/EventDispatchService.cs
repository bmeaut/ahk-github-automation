using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ahk.GitHub.Monitor.Services.EventDispatch;

internal class EventDispatchService : IEventDispatchService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IReadOnlyDictionary<string, List<Type>> handlers;

    public EventDispatchService(IServiceProvider serviceProvider, EventDispatchConfig handlersConfig)
    {
        this.serviceProvider = serviceProvider;

        var handlersList = new Dictionary<string, List<Type>>(StringComparer.OrdinalIgnoreCase);
        foreach ((string GitHubEventName, Type HandlerType) item in handlersConfig.Handlers)
        {
            if (handlersList.TryGetValue(item.GitHubEventName, out List<Type> l))
            {
                l.Add(item.HandlerType);
            }
            else
            {
                handlersList[item.GitHubEventName] = [item.HandlerType];
            }
        }

        handlers = handlersList;
    }

    public async Task Process(string gitHubEventName, string requestBody, WebhookResult webhookResult,
        ILogger logger)
    {
        if (!handlers.TryGetValue(gitHubEventName, out List<Type> handlersForEvent))
        {
            webhookResult.LogInfo($"Event {gitHubEventName} is not of interest");
            logger.LogInformation($"Event {gitHubEventName} is not of interest");
        }
        else
        {
            foreach (Type handlerType in handlersForEvent)
            {
                logger.LogInformation($"Event {gitHubEventName} being handled by {handlerType}");
                await this.executeHandler(requestBody, webhookResult, handlerType, logger);
            }
        }
    }

    private async Task executeHandler(string requestBody, WebhookResult webhookResult, Type handlerType,
        ILogger logger)
    {
        try
        {
            var handler =
                ActivatorUtilities.CreateInstance(
                    serviceProvider, handlerType) as EventHandlers.IGitHubEventHandler;
            EventHandlerResult handlerResult = await handler.Execute(requestBody);

            logger.LogInformation($"{handlerType.Name} result: {handlerResult.Result}");
            webhookResult.LogInfo($"{handlerType.Name} -> {handlerResult.Result}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{handlerType.Name} execution failed");
            webhookResult.LogError(ex, $"{handlerType.Name} -> exception");
        }
    }
}
