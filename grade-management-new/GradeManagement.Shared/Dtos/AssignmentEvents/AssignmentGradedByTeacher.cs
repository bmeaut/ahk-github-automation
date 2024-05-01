namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class AssignmentGradedByTeacher
{
    public string GitHubRepositoryUrl { get; set; }
    public string PullRequestUrl { get; set; }
    public string TeacherGitHubId { get; set; }
    public List<EventScore> Scores { get; set; }
    public DateTimeOffset DateOfGrading { get; set; }
}
