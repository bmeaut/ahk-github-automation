using GradeManagement.Shared.Enums;

namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class PullRequestStatusChanged : AssignmentEventBase
{
    public string PullRequestUrl { get; set; }
    public PullRequestStatus pullRequestStatus {get; set;}
}
