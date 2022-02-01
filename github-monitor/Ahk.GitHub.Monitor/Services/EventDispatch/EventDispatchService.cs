using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ahk.GitHub.Monitor.Services
{
    internal class EventDispatchService : IEventDispatchService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IReadOnlyDictionary<string, List<Type>> handlers;

        public EventDispatchService(IServiceProvider serviceProvider, EventDispatchConfig handlersConfig)
        {
            this.serviceProvider = serviceProvider;

            var handlers = new Dictionary<string, List<Type>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in handlersConfig.Handlers)
            {
                if (handlers.TryGetValue(item.GitHubEventName, out var l))
                    l.Add(item.HandlerType);
                else
                    handlers[item.GitHubEventName] = new List<Type>() { item.HandlerType };
            }

            this.handlers = handlers;
        }

        public async Task Process(string gitHubEventName, string requestBody, WebhookResult webhookResult, ILogger logger)
        {
            if (!this.handlers.TryGetValue(gitHubEventName, out var handlersForEvent))
            {
                webhookResult.LogInfo($"Event {gitHubEventName} is not of interest");
                logger.LogInformation($"Event {gitHubEventName} is not of interest");
            }
            else
            {
                foreach (var handlerType in handlersForEvent)
                {
                    logger.LogInformation($"Event {gitHubEventName} being handled by {handlerType}");
                    await executeHandler(requestBody, webhookResult, handlerType, logger);
                }
            }
        }

        private async Task executeHandler(string requestBody, WebhookResult webhookResult, Type handlerType, ILogger logger)
        {
            try
            {
                var handler = ActivatorUtilities.CreateInstance(serviceProvider, handlerType, logger) as EventHandlers.IGitHubEventHandler;
                var handlerResult = await handler.Execute(requestBody);

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
}
