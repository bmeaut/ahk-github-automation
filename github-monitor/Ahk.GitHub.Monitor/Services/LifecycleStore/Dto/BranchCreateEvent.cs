using System;

namespace Ahk.GitHub.Monitor.Services
{
    public class BranchCreateEvent : LifecycleEvent
    {
        public BranchCreateEvent(string repository, string username, DateTime timestamp, string branch)
            : base(repository, username, timestamp)
        {
            this.Branch = branch;
        }

        public string Branch { get; }
    }
}
