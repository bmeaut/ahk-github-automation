using Ahk.GradeManagement.Shared.Enums;

namespace Ahk.GradeManagement.Shared.Dtos.AssignmentEvents;

public class PullRequestStatusChanged : AssignmentEventBase
{
    public long PullRequestGitHubId { get; set; }
    public string PullRequestUrl { get; set; }
    public PullRequestStatus PullRequestStatus { get; set; }
}
