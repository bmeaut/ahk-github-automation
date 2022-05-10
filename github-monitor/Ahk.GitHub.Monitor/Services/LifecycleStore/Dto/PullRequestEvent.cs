using System;
using System.Collections.Generic;

namespace Ahk.GitHub.Monitor.Services
{
    public class PullRequestEvent : LifecycleEvent
    {
        public PullRequestEvent(string repository, string username, DateTime timestamp, string action, IReadOnlyCollection<string> assignees, string neptun, string htmlUrl, int number)
            : base(repository, username, timestamp)
        {
            this.Action = action;
            this.Assignees = assignees;
            this.Neptun = neptun;
            this.HtmlUrl = htmlUrl;
            this.Number = number;
        }

        public string Action { get; }
        public IReadOnlyCollection<string> Assignees { get; }
        public string Neptun { get; }
        public string HtmlUrl { get; }
        public int Number { get; }
    }
}
