namespace GradeManagement.Data.Models;

public class Subject
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public List<Course> Courses { get; set; }
}
