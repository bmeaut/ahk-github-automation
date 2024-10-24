using System;
using System.Collections.Generic;

namespace Ahk.GitHub.Monitor.Services.EventDispatch
{
    internal class EventDispatchConfig(List<(string GitHubEventName, Type HandlerType)> handlers)
    {
        public IReadOnlyCollection<(string GitHubEventName, Type HandlerType)> Handlers { get; } = handlers;
    }
}
