namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class PullRequestClosed
{
    public string GitHubRepositoryUrl { get; set; }
    public string PullRequestUrl { get; set; }
}
