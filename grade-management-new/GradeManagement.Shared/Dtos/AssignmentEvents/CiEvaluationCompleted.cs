namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class CiEvaluationCompleted
{
    public string GitHubRepositoryUrl { get; set; }
    public string PullRequestUrl { get; set; }
    public string StudentNeptun { get; set; }
    public List<EventScore> Scores { get; set; }
}
