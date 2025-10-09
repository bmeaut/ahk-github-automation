namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

public class PullRequestStatusChanged : StatusEventBase
{
    public required string PullRequestUrl { get; init; }
    public required PullRequestStatus PullRequestStatus { get; init; }
}
