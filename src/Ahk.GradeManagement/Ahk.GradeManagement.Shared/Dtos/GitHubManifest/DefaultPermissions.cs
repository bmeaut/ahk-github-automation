using System.Text.Json.Serialization;

namespace GradeManagement.Shared.Dtos.GitHubManifest;

public class DefaultPermissions
{
    [JsonPropertyName("actions")] public string Actions { get; set; } = "write";
    [JsonPropertyName("administration")] public string Administration { get; set; } = "write";
    [JsonPropertyName("contents")] public string Contents { get; set; } = "write";
    [JsonPropertyName("issues")] public string Issues { get; set; } = "write";
    [JsonPropertyName("metadata")] public string Metadata { get; set; } = "read";
    [JsonPropertyName("pull_requests")] public string PullRequests { get; set; } = "write";
    [JsonPropertyName("workflows")] public string Workflows { get; set; } = "write";
    [JsonPropertyName("members")] public string Members { get; set; } = "read";
}
