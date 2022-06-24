using System;
using Newtonsoft.Json;

namespace Ahk.GradeManagement.Data.Entities
{
    [JsonConverter(typeof(Helper.StatusEventItemJsonConverter))]
    public abstract class StatusEventBase
    {
        protected StatusEventBase(string id, string repository, string username, DateTime timestamp)
        {
            this.Id = id ?? Guid.NewGuid().ToString();
            this.Repository = Normalize.RepoName(repository);
            this.Username = username;
            this.Timestamp = timestamp;
        }

        [JsonProperty("id")]
        public string Id { get; }
        public string Repository { get; }
        public string Username { get; }

        [JsonProperty("$type")]
        public abstract string Type { get; }
        public DateTime Timestamp { get; }
    }
}
