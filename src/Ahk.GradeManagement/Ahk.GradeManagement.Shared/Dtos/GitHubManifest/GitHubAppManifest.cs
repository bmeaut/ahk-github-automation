using System.Text.Json.Serialization;

namespace GradeManagement.Shared.Dtos.GitHubManifest;

public class GitHubAppManifest(string appUrl, string monitorUrl)
{
    [JsonPropertyName("name")] public string Name { get; set; } = "Ahk GitHub Monitor";
    [JsonPropertyName("url")] public string Url { get; set; } = "https://github.com/bmeaut/ahk-github-automation";
    [JsonPropertyName("hook_attributes")] public HookAttributes HookAttributes { get; set; } = new(monitorUrl);
    [JsonPropertyName("redirect_url")] public string RedirectUrl { get; set; } = $"{appUrl}api/github";

    [JsonPropertyName("callback_urls")] public List<string> CallbackUrls { get; set; } = [appUrl];

    [JsonPropertyName("setup_url")] public string SetupUrl { get; set; } = appUrl;

    /*[JsonPropertyName("description")]
    public string Description { get; set; }*/
    [JsonPropertyName("public")] public bool Public { get; set; } = false;

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

    [JsonPropertyName("setup_on_update")] public bool SetupOnUpdate { get; set; } = false;
}
