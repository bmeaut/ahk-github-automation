using System.Collections.Generic;

namespace Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

public class TeacherAssignedEvent : StatusEventBase
{
    public required string PullRequestUrl { get; init; }
    public required IReadOnlyCollection<string> TeacherGitHubIds { get; init; }
}
