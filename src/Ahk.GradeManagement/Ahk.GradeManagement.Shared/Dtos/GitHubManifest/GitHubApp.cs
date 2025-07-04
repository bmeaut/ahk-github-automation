using System.Text.Json;
using System.Text.Json.Serialization;

namespace GradeManagement.Shared.Dtos.GitHubManifest;

public class GitHubApp
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("slug")] public string Slug { get; set; }

    [JsonPropertyName("node_id")] public string NodeId { get; set; }

    [JsonPropertyName("client_id")] public string ClientId { get; set; }

    [JsonPropertyName("owner")] public GitHubUser Owner { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("description")] public string Description { get; set; }

    [JsonPropertyName("external_url")] public string ExternalUrl { get; set; }

    [JsonPropertyName("html_url")] public string HtmlUrl { get; set; }

    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("permissions")] public GitHubAppPermissions Permissions { get; set; }

    [JsonPropertyName("events")] public List<string> Events { get; set; }

    [JsonPropertyName("installations_count")]
    public int InstallationsCount { get; set; }

    [JsonPropertyName("client_secret")] public string ClientSecret { get; set; }

    [JsonPropertyName("webhook_secret")] public string WebhookSecret { get; set; }

    [JsonPropertyName("pem")] public string Pem { get; set; }
}

public class GitHubAppPermissions
{
    [JsonPropertyName("issues")] public string Issues { get; set; }

    [JsonPropertyName("checks")] public string Checks { get; set; }

    [JsonPropertyName("metadata")] public string Metadata { get; set; }

    [JsonPropertyName("contents")] public string Contents { get; set; }

    [JsonPropertyName("deployments")] public string Deployments { get; set; }

    [JsonExtensionData] public Dictionary<string, JsonElement> AdditionalProperties { get; set; }
}
