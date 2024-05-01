namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class AssignmentGradedByTeacher : AssignmentEventBase
{
    public string PullRequestUrl { get; set; }
    public string TeacherGitHubId { get; set; }
    public List<EventScore> Scores { get; set; }
    public DateTimeOffset DateOfGrading { get; set; }
}
