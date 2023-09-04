using System;

namespace Ahk.GradeManagement.Data.Entities
{
    
    public class BranchCreateEvent : StatusEventBase
    {
        public const string TypeName = "BranchCreateEvent";

        public override string Type { get => TypeName; }
        public string Branch { get; set; }
    }
}
