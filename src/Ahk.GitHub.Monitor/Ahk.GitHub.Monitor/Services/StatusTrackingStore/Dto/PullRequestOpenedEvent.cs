using System;

namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

public class PullRequestOpenedEvent : StatusEventBase
{
    public long PullRequestGitHubId { get; init; }
    public required string PullRequestUrl { get; init; }
    public required string BranchName { get; init; }
    public required DateTimeOffset OpeningDate { get; init; }
}
