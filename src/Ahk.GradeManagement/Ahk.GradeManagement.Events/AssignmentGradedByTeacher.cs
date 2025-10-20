namespace Ahk.GradeManagement.Events;

public record AssignmentGradedByTeacher
{
    public required string GitHubRepositoryUrl { get; init; }
    public required long PullRequestGitHubId { get; init; }
    public required string PullRequestUrl { get; init; }
    public required string TeacherGitHubId { get; init; }
    public required Dictionary<int, double> Scores { get; init; }
    public required DateTimeOffset DateOfGrading { get; init; }
}
