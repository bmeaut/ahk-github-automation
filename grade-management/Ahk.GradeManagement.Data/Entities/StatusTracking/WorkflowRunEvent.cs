using System;

namespace Ahk.GradeManagement.Data.Entities
{
    [Newtonsoft.Json.JsonConverter(typeof(Helper.DisabledJsonConverter))]
    public class WorkflowRunEvent : StatusEventBase
    {
        public const string TypeName = "WorkflowRunEvent";

        public WorkflowRunEvent(string id, string repository, DateTime timestamp, string conclusion)
            : base(id, repository, timestamp)
        {
            this.Conclusion = conclusion;
        }

        public override string Type => TypeName;
        public string Conclusion { get; }
    }
}
