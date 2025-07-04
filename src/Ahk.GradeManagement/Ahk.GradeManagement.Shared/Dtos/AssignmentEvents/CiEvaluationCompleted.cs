namespace Ahk.GradeManagement.Shared.Dtos.AssignmentEvents;

public class CiEvaluationCompleted : AssignmentEventBase
{
    public string PullRequestUrl { get; set; }
    public string StudentNeptun { get; set; }
    public Dictionary<int, double> Scores { get; set; } //order, score
    public string CiApiKey { get; set; }
}
