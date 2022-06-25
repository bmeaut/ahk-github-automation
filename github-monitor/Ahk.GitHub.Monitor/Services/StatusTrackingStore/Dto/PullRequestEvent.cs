using System;
using System.Collections.Generic;

namespace Ahk.GitHub.Monitor.Services
{
    public class PullRequestEvent : StatusEventBase
    {
        public PullRequestEvent(string repository, DateTime timestamp, string action, IReadOnlyCollection<string> assignees, string neptun, string htmlUrl, int number)
            : base(repository, timestamp)
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
