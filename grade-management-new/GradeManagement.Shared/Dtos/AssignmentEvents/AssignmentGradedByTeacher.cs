namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class AssignmentGradedByTeacher : AssignmentEventBase
{
    public string PullRequestUrl { get; set; }
    public string TeacherGitHubId { get; set; }
    public Dictionary<int, double> Scores { get; set; }//order, score
    public DateTimeOffset DateOfGrading { get; set; }
}
