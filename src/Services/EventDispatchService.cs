using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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
                webhookResult.LogInfo($"Event {gitHubEventName} is not of interrest");
            }
            else
            {
                foreach (var handlerType in handlersForEvent)
                {
                    var handler = serviceProvider.GetRequiredService(handlerType) as EventHandlers.IGitHubEventHandler;
                    await handler.Execute(requestBody, webhookResult);
                }
            }
        }
    }
}
