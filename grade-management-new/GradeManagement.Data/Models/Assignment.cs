namespace GradeManagement.Data.Models;

public class Assignment : ISoftDelete
{
    public long Id { get; set; }
    public string GithubRepoName { get; set; }
    public Student Student { get; set; }
    public long StudentId { get; set; }
    public Exercise Exercise { get; set; }
    public long ExcerciseId { get; set; }
    public List<PullRequest> PullRequests { get; set; }
    public List<Score> Scores { get; set; }
    public List<AssignmentEvent> AssignmentEvents { get; set; }
    public bool IsDeleted { get; set; }
}
