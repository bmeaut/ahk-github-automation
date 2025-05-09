using System.Text.Json.Serialization;

namespace GradeManagement.Shared.Dtos.GitHubManifest;

public class HookAttributes
{
    public HookAttributes()
    {
        string monitorUrl = Environment.GetEnvironmentVariable("MONITOR_URL");
        Url = $"{monitorUrl}api/github-webhooks";
    }

    [JsonPropertyName("url")] public string Url { get; set; }
    [JsonPropertyName("active")] public bool Active { get; set; } = true;
}
