namespace GradeManagement.Data.Models;

public class Student : ISoftDelete
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string? NeptunCode { get; set; }
    public string? GithubId { get; set; }
    public string? MoodleId { get; set; }
    public List<GroupStudent> GroupStudents { get; set; }
    public List<Assignment> Assignments { get; set; }
    public bool IsDeleted { get; set; }
}
