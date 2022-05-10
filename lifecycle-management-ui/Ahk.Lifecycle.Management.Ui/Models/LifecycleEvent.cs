using System.Text.Json.Serialization;

namespace Ahk.Lifecycle.Management.Ui.Models
{
    public class LifecycleEvent
    {
        public LifecycleEvent(string id, string repository, string username, string type, DateTime timestamp, string action, string branch, string htmlUrl, int number)
        {
            this.Id = id;
            this.Repository = repository;
            this.Username = username;
            this.Type = type;
            this.Timestamp = timestamp;
            this.Action = action;
            this.Branch = branch;
            this.HtmlUrl = htmlUrl;
            this.Number = number;
        }

        [JsonPropertyName("id")]
        public string Id { get; }
        public string Repository { get; }
        public string Username { get; }

        [JsonPropertyName("$type")]
        public string Type { get; }
        public DateTime Timestamp { get; }
        public string Action { get; }
        public string Branch { get; }
        public string HtmlUrl { get; }
        public int Number { get; }
    }
}
