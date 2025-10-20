namespace Ahk.GradeManagement.Events;

public record PullRequestStatusChanged
{
    public required string GitHubRepositoryUrl { get; init; }
    public required long PullRequestGitHubId { get; init; }
    public required string PullRequestUrl { get; init; }
    public required PullRequestStatus PullRequestStatus { get; init; }
}
