namespace GradeManagement.Shared.Dtos.Request;

public class Student
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string? NeptunCode { get; set; }
    public string? GithubId { get; set; }
    public List<long> GroupIds { get; set; }
}
