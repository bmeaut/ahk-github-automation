namespace Ahk.GradeManagement.Shared.Dtos.AssignmentEvents;

public class AssignmentGradedByTeacher : AssignmentEventBase
{
    public long PullRequestGitHubId { get; set; }
    public string PullRequestUrl { get; set; }
    public string TeacherGitHubId { get; set; }
    public Dictionary<int, double> Scores { get; set; } //order, score
    public DateTimeOffset DateOfGrading { get; set; }
}
