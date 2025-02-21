namespace Ahk.GitHub.Monitor.Config;

public class GitHubMonitorConfig
{
    private static string name = "GitHubMonitorConfig";

    public string GitHubAppId { get; set; }
    public string GitHubAppPrivateKey { get; set; }
    public string GitHubWebhookSecret { get; set; }

    public static string GetSectionName(string gitHubOrganisationName) => name + ":" + gitHubOrganisationName;
}
