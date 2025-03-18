namespace GradeManagement.Shared.Dtos.GitHubManifest;

public class HookAttributes
{
    public string Url { get; set; } = Environment.GetEnvironmentVariable("MONITOR_URL");
    public bool Active { get; set; } = true;
}
