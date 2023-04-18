using System;

namespace Ahk.GradeManagement.Data.Entities
{
    
    public class WorkflowRunEvent : StatusEventBase
    {
        public const string TypeName = "WorkflowRunEvent";

        public override string Type { get => TypeName; set { } }
        public string Conclusion { get; set; }
    }
}
