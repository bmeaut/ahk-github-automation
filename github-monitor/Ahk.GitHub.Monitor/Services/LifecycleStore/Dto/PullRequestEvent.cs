using System;

namespace Ahk.GitHub.Monitor.Services
{
    public class PullRequestEvent : LifecycleEvent
    {
        public PullRequestEvent(string repository, string username, DateTime timestamp, string action, string[] assignees, string neptun)
            : base(repository, username, timestamp)
        {
            this.Action = action;
            this.Assignees = assignees;
            this.Neptun = neptun;
        }

        public string Action { get; }
        public string[] Assignees { get; }
        public string Neptun { get; }
    }
}
