using System.Text.Json.Serialization;

namespace Ahk.GradeManagement.Shared.Dtos.GitHubManifest;

public class HookAttributes
{
    public HookAttributes(string monitorUrl)
    {
        Url = $"{monitorUrl}";
    }

    [JsonPropertyName("url")] public string Url { get; set; }
    [JsonPropertyName("active")] public bool Active { get; set; } = true;
}
