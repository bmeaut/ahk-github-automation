using System;

namespace Ahk.GitHub.Monitor.Services
{
    public class WorkflowRunEvent : StatusEventBase
    {
        public WorkflowRunEvent(string repository, string username, DateTime timestamp, string conclusion)
            : base(repository, username, timestamp)
        {
            this.Conclusion = conclusion;
        }

        public string Conclusion { get; }
    }
}
