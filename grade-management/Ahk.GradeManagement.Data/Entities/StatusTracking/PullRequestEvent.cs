using System;
using System.Collections.Generic;
using Ahk.GradeManagement.Data.Entities.StatusTracking;

namespace Ahk.GradeManagement.Data.Entities
{
    
    public class PullRequestEvent : StatusEventBase
    {
        public const string TypeName = "PullRequestEvent";

        public override string Type {get => TypeName; }
        public string Action { get; set; }
        public string Neptun { get; set; }
        public string HtmlUrl { get; set; }
        public int Number { get; set; }

        public ICollection<Assignee> Assignees { get; set; }
    }
}
