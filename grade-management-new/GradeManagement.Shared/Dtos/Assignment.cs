namespace GradeManagement.Shared.Dtos;

public class Assignment
{
    public long Id { get; set; }
    public string GithubRepoName { get; set; }
    public long StudentId { get; set; }
    public long ExcerciseId { get; set; }
}
