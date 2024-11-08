using System;
using System.Collections.Generic;

namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

public class PullRequestOpenedEvent(
    string gitHubRepositoryUrl,
    DateTimeOffset timestamp,
    string branchName,
    string pullRequestUrl)
    : StatusEventBase(gitHubRepositoryUrl)
{
    public string PullRequestUrl { get; } = pullRequestUrl;
    public string BranchName { get; } = branchName;
    public DateTimeOffset OpeningDate { get; } = timestamp;
}
