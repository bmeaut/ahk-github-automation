using System;
using System.Collections.Generic;
using Ahk.GitHub.Monitor.EventHandlers;
using Ahk.GitHub.Monitor.EventHandlers.BaseAndUtils;
using Microsoft.Extensions.DependencyInjection;

namespace Ahk.GitHub.Monitor.Services.EventDispatch;

internal class EventDispatchConfigBuilder(IServiceCollection services)
{
    private readonly List<(string GitHubEventName, Type HandlerType)> handlers = [];

    public EventDispatchConfigBuilder Add<T>(string gitHubEventName)
        where T : IGitHubEventHandler
    {
        services.AddTransient(typeof(T));
        handlers.Add((gitHubEventName, typeof(T)));
        return this;
    }

    public EventDispatchConfig Build() => new(handlers);
}
