namespace Ahk.GradeManagement.Events;

public record CiEvaluationCompleted
{
    public required string GitHubRepositoryUrl { get; init; }
    public required long PullRequestGitHubId { get; init; }
    public required string PullRequestUrl { get; init; }
    public required string StudentNeptun { get; init; }
    public required Dictionary<int, double> Scores { get; init; }
    public required string CiApiKey { get; init; }
}
