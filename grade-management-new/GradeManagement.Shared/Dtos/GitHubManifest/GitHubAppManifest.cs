using System.Text.Json.Serialization;

namespace GradeManagement.Shared.Dtos.GitHubManifest;

public class GitHubAppManifest
{
    public GitHubAppManifest()
    {
        string appUrl = Environment.GetEnvironmentVariable("APP_URL");
        RedirectUrl = $"{appUrl}api/github";
    }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Ahk GitHub Monitor";
    [JsonPropertyName("url")]
    public string Url { get; set; } = "https://github.com/bmeaut/ahk-github-automation";
    [JsonPropertyName("hook_attributes")]
    public HookAttributes HookAttributes { get; set; } = new();
    [JsonPropertyName("redirect_url")]
    public string RedirectUrl { get; set; }
    [JsonPropertyName("callback_urls")]
    public List<string> CallbackUrls { get; set; } = new();
    /*[JsonPropertyName("setup_url")]
    public string SetupUrl { get; set; }*/
    /*[JsonPropertyName("description")]
    public string Description { get; set; }*/
    [JsonPropertyName("public")]
    public bool Public { get; set; } = false;

    [JsonPropertyName("default_events")]
    public List<string> DefaultEvents { get; set; } =
    [
        "create", "issue_comment", "label", "pull_request", "pull_request_review", "pull_request_review_comment",
        "repository", "workflow_run"
    ];
    [JsonPropertyName("default_permissions")]
    public DefaultPermissions DefaultPermissions { get; set; } = new DefaultPermissions();
    [JsonPropertyName("request_oauth_on_install")]
    public bool RequestOAuthOnInstall { get; set; } = false;
    [JsonPropertyName("setup_on_update")]
    public bool SetupOnUpdate { get; set; } = false;
}
