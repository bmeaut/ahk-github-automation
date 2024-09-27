namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class CiEvaluationCompleted : AssignmentEventBase
{
    public string PullRequestUrl { get; set; }
    public string StudentNeptun { get; set; }
    public List<EventScore> Scores { get; set; }
    public string CiApiKey { get; set; }
}
