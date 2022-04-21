using System;

namespace Ahk.GitHub.Monitor.Services
{
    public abstract class LifecycleEvent
    {
        protected LifecycleEvent(string repository, string username, DateTime timestamp)
        {
            this.Repository = repository;
            this.Username = username;
            this.Timestamp = timestamp;
        }

        public string Repository { get; }
        public string Username { get; }
        public DateTime Timestamp { get; }
    }
}
