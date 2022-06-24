using System;
using System.Collections.Generic;

namespace Ahk.GradeManagement.Data.Entities
{
    public class PullRequestEvent : StatusEventBase
    {
        public PullRequestEvent(string id, string repository, string username, DateTime timestamp, string action, IReadOnlyCollection<string> assignees, string neptun, string htmlUrl, int number)
            : base(id, repository, username, timestamp)
        {
            this.Action = action;
            this.Assignees = assignees;
            this.Neptun = Normalize.Neptun(neptun);
            this.HtmlUrl = htmlUrl;
            this.Number = number;
        }

        public override string Type { get; } = "PullRequestEvent";
        public string Action { get; }
        public IReadOnlyCollection<string> Assignees { get; }
        public string Neptun { get; }
        public string HtmlUrl { get; }
        public int Number { get; }
    }
}
