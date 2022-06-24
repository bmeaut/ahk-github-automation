using System;

namespace Ahk.GradeManagement.Data.Entities
{
    public class WorkflowRunEvent : StatusEventBase
    {
        public WorkflowRunEvent(string id, string repository, string username, DateTime timestamp, string conclusion)
            : base(id, repository, username, timestamp)
        {
            this.Conclusion = conclusion;
        }

        public override string Type { get; } = "WorkflowRunEvent";
        public string Conclusion { get; }
    }
}
