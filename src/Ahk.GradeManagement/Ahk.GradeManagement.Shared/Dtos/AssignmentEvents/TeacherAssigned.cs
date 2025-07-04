namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class TeacherAssigned : AssignmentEventBase
{
    public string PullRequestUrl { get; set; }
    public IReadOnlyCollection<string> TeacherGitHubIds { get; set; }
}
