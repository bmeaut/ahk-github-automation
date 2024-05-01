namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class TeacherAssigned : AssignmentEventBase
{
    public string PullRequestUrl { get; set; }
    public string TeacherGitHubId { get; set; }
}
