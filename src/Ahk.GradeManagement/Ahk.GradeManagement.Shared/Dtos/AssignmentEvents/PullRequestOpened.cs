namespace Ahk.GradeManagement.Shared.Dtos.AssignmentEvents;

public class PullRequestOpened : AssignmentEventBase
{
    public string PullRequestUrl { get; set; }
    public string BranchName { get; set; }
    public DateTimeOffset OpeningDate { get; set; }
}
