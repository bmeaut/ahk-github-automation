namespace Ahk.GradeManagement.Shared.Dtos;

public class Dashboard
{
    public string AssignmentName { get; set; }
    public string GithubRepoUrl { get; set; }
    public string? StudentNeptun { get; set; }
    public string ExerciseName { get; set; }
    public string CourseName { get; set; }
    public List<PullRequestForDashboard> PullRequests { get; set; }
}
