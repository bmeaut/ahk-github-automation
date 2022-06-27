using System;

namespace Ahk.GitHub.Monitor.Services
{
    public class BranchCreateEvent : StatusEventBase
    {
        public BranchCreateEvent(string repository, DateTime timestamp, string branch)
            : base(repository, timestamp)
        {
            this.Branch = branch;
        }

        public string Branch { get; }
    }
}
