namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

public class PullRequestStatusChanged(
    string gitHubRepositoryUrl,
    string pullRequestUrl,
    PullRequestStatus pullRequestStatus)
    : StatusEventBase(gitHubRepositoryUrl)
{
    public string PullRequestUrl { get; } = pullRequestUrl;
    public PullRequestStatus PullRequestStatus { get; } = pullRequestStatus;
}
