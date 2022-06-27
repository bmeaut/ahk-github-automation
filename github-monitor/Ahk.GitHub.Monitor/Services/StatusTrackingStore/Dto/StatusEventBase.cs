using System;

namespace Ahk.GitHub.Monitor.Services
{
    public abstract class StatusEventBase
    {
        protected StatusEventBase(string repository, DateTime timestamp)
        {
            this.Repository = repository;
            this.Timestamp = timestamp;
        }

        public string Repository { get; }
        public DateTime Timestamp { get; }
    }
}
