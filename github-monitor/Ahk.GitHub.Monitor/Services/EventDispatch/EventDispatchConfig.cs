using System;
using System.Collections.Generic;

namespace Ahk.GitHub.Monitor.Services
{
    internal class EventDispatchConfig
    {
        public EventDispatchConfig(List<(string GitHubEventName, Type HandlerType)> handlers)
            => this.Handlers = handlers;

        public IReadOnlyCollection<(string GitHubEventName, Type HandlerType)> Handlers { get; }
    }
}
