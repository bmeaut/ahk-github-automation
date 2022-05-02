using Newtonsoft.Json;

namespace Ahk.Lifecycle.Management.DAL.Entities
{
    public abstract class LifecycleEvent
    {
        protected LifecycleEvent(string id, string repository, string username, DateTime timestamp)
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
        public abstract string Type { get; }
        public DateTime Timestamp { get; }
    }
}
