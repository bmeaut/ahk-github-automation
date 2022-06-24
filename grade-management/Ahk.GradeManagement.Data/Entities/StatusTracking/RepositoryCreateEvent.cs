using System;

namespace Ahk.GradeManagement.Data.Entities
{
    [Newtonsoft.Json.JsonConverter(typeof(Helper.DisabledJsonConverter))]
    public class RepositoryCreateEvent : StatusEventBase
    {
        public const string TypeName = "RepositoryCreateEvent";
        public RepositoryCreateEvent(string id, string repository, string username, DateTime timestamp)
            : base(id, repository, username, timestamp)
        {
        }

        public override string Type => TypeName;
    }
}
