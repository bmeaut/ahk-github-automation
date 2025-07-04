using GradeManagement.Data.Models.Interfaces;

namespace GradeManagement.Data.Models;

public class Assignment : ISoftDelete, ITenant
{
    public long Id { get; set; }
    public string GithubRepoName { get; set; }
    public string GithubRepoUrl { get; set; }
    public Student Student { get; set; }
    public long StudentId { get; set; }
    public Exercise Exercise { get; set; }
    public long ExerciseId { get; set; }
    public List<PullRequest> PullRequests { get; set; }
    public List<AssignmentLog> AssignmentLogs { get; set; }
    public bool IsDeleted { get; set; }
    public long SubjectId { get; set; }
}
