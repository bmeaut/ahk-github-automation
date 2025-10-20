namespace Ahk.GradeManagement.Events;

public record AssignmentAccepted
{
    public required string GitHubRepositoryUrl { get; init; }
}
