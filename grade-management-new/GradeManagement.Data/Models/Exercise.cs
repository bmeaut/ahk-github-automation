namespace GradeManagement.Data.Models;

public class Exercise
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string GithubPrefix { get; set; }
    public Course Course { get; set; }
    public long CourseId { get; set; }
    public List<Assignment> Assignments { get; set; }
}
