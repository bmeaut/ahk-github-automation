using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services
{
    internal class EventDispatchService : IEventDispatchService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IReadOnlyDictionary<string, List<Type>> handlers;

        public EventDispatchService(IServiceProvider serviceProvider, EventDispatchConfigBuilder builder)
        {
            this.serviceProvider = serviceProvider;

            var handlers = new Dictionary<string, List<Type>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in builder.GetAll())
            {
                if (handlers.TryGetValue(item.GitHubEventName, out var l))
                    l.Add(item.HandlerType);
                else
                    handlers[item.GitHubEventName] = new List<Type>() { item.HandlerType };
            }

            this.handlers = handlers;
        }

        public async Task Process(string gitHubEventName, string requestBody, WebhookResult webhookResult)
        {
            if (!this.handlers.TryGetValue(gitHubEventName, out var handlersForEvent))
            {
                webhookResult.LogInfo($"Event {gitHubEventName} is not of interest");
            }
            else
            {
                foreach (var handlerType in handlersForEvent)
                    await executeHandler(requestBody, webhookResult, handlerType);
            }
        }

        private async Task executeHandler(string requestBody, WebhookResult webhookResult, Type handlerType)
        {
            try
            {
                var handler = serviceProvider.GetRequiredService(handlerType) as EventHandlers.IGitHubEventHandler;
                var handlerResult = await handler.Execute(requestBody);
                webhookResult.LogInfo($"{handlerType.Name} -> {handlerResult.Result}");
            }
            catch (Exception ex)
            {
                webhookResult.LogError(ex, $"{handlerType.Name} -> exception");
            }
        }
    }
}
