namespace Ahk.GradeManagement.Events;

public record TeacherAssigned
{
    public required string GitHubRepositoryUrl { get; init; }
    public required long PullRequestGitHubId { get; init; }
    public required string PullRequestUrl { get; init; }
    public required IReadOnlyCollection<string> TeacherGitHubIds { get; init; }
}
