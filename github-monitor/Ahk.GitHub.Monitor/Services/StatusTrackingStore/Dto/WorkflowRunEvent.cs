using System;

namespace Ahk.GitHub.Monitor.Services
{
    public class WorkflowRunEvent : StatusEventBase
    {
        public WorkflowRunEvent(string repository, DateTime timestamp, string conclusion)
            : base(repository, timestamp)
        {
            this.Conclusion = conclusion;
        }

        public string Conclusion { get; }
    }
}
