using Ahk.GradeManagement.Shared.Enums;

namespace Ahk.GradeManagement.Shared.Dtos.AssignmentEvents;

public class PullRequestStatusChanged : AssignmentEventBase
{
    public string PullRequestUrl { get; set; }
    public PullRequestStatus pullRequestStatus { get; set; }
}
