namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class TeacherAssigned
{
    public string GitHubRepositoryUrl { get; set; }
    public string TeacherGitHubId { get; set; }
    public string PullRequestUrl { get; set; }
}
