using System;
using System.Collections.Generic;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Ahk.GitHub.Monitor.Services
{
    internal class EventDispatchConfigBuilder
    {
        private readonly IServiceCollection services;
        private readonly List<(string GitHubEventName, Type HandlerType)> handlers = new List<(string GitHubEventName, Type HandlerType)>();

        public EventDispatchConfigBuilder(IServiceCollection services)
        {
            this.services = services;
        }

        public EventDispatchConfigBuilder Add<T>(string gitHubEventName)
            where T : IGitHubEventHandler
        {
            this.services.AddTransient(typeof(T));
            this.handlers.Add((gitHubEventName, typeof(T)));
            return this;
        }

        public EventDispatchConfig Build() => new EventDispatchConfig(this.handlers);
    }
}
