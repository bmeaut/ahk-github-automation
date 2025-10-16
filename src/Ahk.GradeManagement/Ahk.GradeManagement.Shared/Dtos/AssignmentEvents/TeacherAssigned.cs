namespace Ahk.GradeManagement.Shared.Dtos.AssignmentEvents;

public class TeacherAssigned : AssignmentEventBase
{
    public long PullRequestGitHubId { get; set; }
    public string PullRequestUrl { get; set; }
    public IReadOnlyCollection<string> TeacherGitHubIds { get; set; }
}
