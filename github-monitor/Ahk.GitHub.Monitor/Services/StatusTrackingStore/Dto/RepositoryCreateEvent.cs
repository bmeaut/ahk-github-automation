using System;

namespace Ahk.GitHub.Monitor.Services
{
    public class RepositoryCreateEvent : StatusEventBase
    {
        public RepositoryCreateEvent(string repository, string username, DateTime timestamp)
            : base(repository, username, timestamp)
        {
        }
    }
}
