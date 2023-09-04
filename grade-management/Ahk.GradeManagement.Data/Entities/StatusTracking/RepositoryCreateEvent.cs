using System;

namespace Ahk.GradeManagement.Data.Entities
{
    
    public class RepositoryCreateEvent : StatusEventBase
    {
        public const string TypeName = "RepositoryCreateEvent";

        public override string Type { get => TypeName; }
    }
}
