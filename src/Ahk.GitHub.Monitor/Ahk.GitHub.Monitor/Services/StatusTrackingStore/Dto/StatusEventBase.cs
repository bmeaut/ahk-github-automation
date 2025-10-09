namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

public abstract class StatusEventBase
{
    public required string GitHubRepositoryUrl { get; init; }
}
