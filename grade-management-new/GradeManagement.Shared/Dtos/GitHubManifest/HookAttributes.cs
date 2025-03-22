using System.Text.Json.Serialization;

namespace GradeManagement.Shared.Dtos.GitHubManifest;

public class HookAttributes
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "https://monitor.mm-home.eu/api/github-webhooks";
    [JsonPropertyName("active")]
    public bool Active { get; set; } = true;
}
