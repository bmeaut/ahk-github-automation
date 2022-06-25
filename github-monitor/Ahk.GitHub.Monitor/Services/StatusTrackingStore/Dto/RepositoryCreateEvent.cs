using System;

namespace Ahk.GitHub.Monitor.Services
{
    public class RepositoryCreateEvent : StatusEventBase
    {
        public RepositoryCreateEvent(string repository, DateTime timestamp)
            : base(repository, timestamp)
        {
        }
    }
}
