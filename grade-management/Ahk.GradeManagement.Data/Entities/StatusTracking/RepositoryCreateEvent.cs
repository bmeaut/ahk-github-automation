using System;

namespace Ahk.GradeManagement.Data.Entities
{
    public class RepositoryCreateEvent : StatusEventBase
    {
        public RepositoryCreateEvent(string id, string repository, string username, DateTime timestamp)
            : base(id, repository, username, timestamp)
        {
        }

        public override string Type { get; } = "RepositoryCreateEvent";
    }
}
