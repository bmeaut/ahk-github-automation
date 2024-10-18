using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

public class TeacherAssignedEvent(
    string gitHubRepositoryUrl,
    string pullRequestUrl,
    IReadOnlyCollection<string> teacherGithubIds)
    : StatusEventBase(gitHubRepositoryUrl)
{
    public string PullRequestUrl { get; } = pullRequestUrl;
    public IReadOnlyCollection<string> TeacherGitHubIds { get; } = teacherGithubIds;
}
