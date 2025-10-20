namespace Ahk.GradeManagement.Events;

public record PullRequestOpened
{
    public required string GitHubRepositoryUrl { get; init; }
    public required long PullRequestGitHubId { get; init; }
    public required string PullRequestUrl { get; init; }
    public required string BranchName { get; init; }
    public required DateTimeOffset OpeningDate { get; init; }
}
