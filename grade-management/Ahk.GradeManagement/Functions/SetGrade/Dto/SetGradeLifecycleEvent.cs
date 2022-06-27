using System;
using Newtonsoft.Json;

namespace Ahk.GradeManagement.SetGrade
{
    public class SetGradeLifecycleEvent
    {
        public SetGradeLifecycleEvent(string id, string repository, string username, DateTime timestamp)
        {
            this.Id = id ?? Guid.NewGuid().ToString();
            this.Repository = repository;
            this.Username = username;
            this.Timestamp = timestamp;
        }

        [JsonProperty("id")]
        public string Id { get; }
        public string Repository { get; }
        public string Username { get; }

        [JsonProperty("$type")]
        public string Type { get; } = "SetGradeEvent";
        public DateTime Timestamp { get; }
    }
}
