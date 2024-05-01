namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class PullRequestOpened
{
    public string GitHubRepositoryUrl { get; set; }
    public string PullRequestUrl { get; set; }
    public string BranchName { get; set; }
    public DateTimeOffset OpeningDate { get; set; }
}
