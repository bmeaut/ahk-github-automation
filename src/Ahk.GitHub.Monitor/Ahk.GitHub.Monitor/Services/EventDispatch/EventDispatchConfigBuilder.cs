using Ahk.GitHub.Monitor.EventHandlers.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;

namespace Ahk.GitHub.Monitor.Services.EventDispatch;

internal class EventDispatchConfigBuilder(IServiceCollection services)
{
    private readonly List<(string GitHubEventName, Type HandlerType)> _handlers = [];

    public EventDispatchConfigBuilder Add<T>()
        where T : IGitHubEventHandler
    {
        services.AddTransient(typeof(T));
        _handlers.Add((T.GitHubWebhookEventName, typeof(T)));
        return this;
    }

    public EventDispatchConfig Build() => new(_handlers);
}
