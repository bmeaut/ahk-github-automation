namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

public abstract class StatusEventBase(string gitHubRepositoryUrl)
{
    public string GitHubRepositoryUrl { get; } = gitHubRepositoryUrl;
}
